﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using VirtualClassroom.Services.Models;
using VirtualClassroom.Services.Views;
using System.Security.Cryptography;

namespace VirtualClassroom.Services.Services
{
    /// <summary>
    /// Implementation of the IStudentService
    /// </summary>
    
    //enable sessions
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Single)]
    public class StudentService : IStudentService
    {
        VirtualClassroomEntities entityContext = new VirtualClassroomEntities();
        private bool isLogged = false;          //stores login state

        /// <summary>
        /// Checks if the client is authenticated
        /// </summary>
        private void CheckAuthentication()
        {
            if (isLogged == false)
            {
                throw new FaultException("Не сте влезли в системата");
            }
        }

        /// <summary>
        /// Logs a student into the system
        /// </summary>
        /// <param name="usernameCrypt">Encrypted username</param>
        /// <param name="passwordCrypt">Encrypted password</param>
        /// <param name="secret">The key to decrypt with</param>
        /// <returns>Student information Iif successfull login)</returns>
        public Student LoginStudent(string usernameCrypt, string passwordCrypt, string secret)
        {
            if (string.IsNullOrWhiteSpace(usernameCrypt) || string.IsNullOrEmpty(usernameCrypt)
                || string.IsNullOrWhiteSpace(passwordCrypt) || string.IsNullOrEmpty(passwordCrypt)
                || string.IsNullOrWhiteSpace(secret) || string.IsNullOrEmpty(secret))
            {
                return null;
            }

            //decrypt login details
            string username = Crypto.DecryptStringAES(usernameCrypt, secret);
            string password = Crypto.DecryptStringAES(passwordCrypt, secret);

            if (entityContext.Students.Count(s => s.Username == username) == 0)
            {
                return null;
            }

            Student entity = entityContext.Students.Where(s => s.Username == username).First();
            if (BCrypt.Net.BCrypt.Verify(password, entity.PasswordHash))
            {
                //password is valid
                isLogged = true;
                return entity;
            }

            return null;
        }

        /// <summary>
        /// Gets all lessons by a given student 
        /// </summary>
        /// <param name="studentId">The student id</param>
        /// <returns>The lessons for this student, encapsulated in a LessonView list</returns>
        public List<LessonView> GetLessonViewsByStudent(int studentId)
        {
            CheckAuthentication();

            int classId = (from c in entityContext.Classes.Include("Students")
                           where c.Students.Any(s => s.Id == studentId)
                           select c.Id).First();

            var addedHomeworks = this.GetHomeworksByStudent(studentId).Select(h => h.LessonId);

            return (
                       from c in entityContext.Classes.Include("Subjects")
                       from s in c.Subjects
                       join l in entityContext.Lessons
                           on s.Id equals l.SubjectId
                       where c.Id == classId
                       select new LessonView()
                                  {
                                      Id = l.Id,
                                      Date = l.Date,
                                      HomeworkDeadline = l.HomeworkDeadline,
                                      HasHomework = (l.HomeworkDeadline == null) ? false : true,
                                      SentHomework = (addedHomeworks.Contains(l.Id)) ? true : false,
                                      Name = l.Name,
                                      Subject = s.Name
                                  }).ToList();
        }

        /// <summary>
        /// Downloads a lesson's content
        /// </summary>
        /// <param name="lessonId">The lesson id</param>
        /// <returns>The lesson's raw content and file name, encapsulated in a File structure</returns>
        public File DownloadLessonContent(int lessonId)
        {
            CheckAuthentication();

            Lesson lesson = (from l in entityContext.Lessons
                             where l.Id == lessonId
                             select l).First();

            return new File(lesson.ContentFilename, lesson.Content);
        }

        /// <summary>
        /// Downloads a lesson's homework
        /// </summary>
        /// <param name="lessonId">The lesson id</param>
        /// <returns>The homework's raw content and filename, encapsulated in a File structure</returns>
        public File DownloadLessonHomework(int lessonId)
        {
            CheckAuthentication();

            Lesson lesson = (from l in entityContext.Lessons
                             where l.Id == lessonId
                             select l).First();

            return new File(lesson.HomeworkFilename, lesson.HomeworkContent);
        }

        /// <summary>
        /// Downloads a homework that has been sent by the student
        /// </summary>
        /// <param name="studentId">The id of the student</param>
        /// <param name="lessonId">The id of the lesson</param>
        /// <returns>The homework, encapsulated in a File structure</returns>
        public File DownloadSentHomework(int studentId, int lessonId)
        {
            var addedHomeworks = this.GetHomeworksByStudent(studentId);
            if(addedHomeworks.Any(h => h.LessonId == lessonId))
            {
                var homework = addedHomeworks.Where(h => h.LessonId == lessonId).FirstOrDefault();
                return new File(homework.Filename, homework.Content);
            }

            throw new FaultException("Такова домашно не съществува");
        }

        /// <summary>
        /// Adds a homework to the database
        /// </summary>
        /// <param name="homework">The homework information to add</param>
        public bool AddHomework(Homework homework)
        {
            CheckAuthentication();

            var lesson = entityContext.Lessons
                .Where(l => l.Id == homework.LessonId).FirstOrDefault();
            if(lesson.HomeworkDeadline < DateTime.Now)
            {
                return false;
            }

            homework.Date = DateTime.Now;
            entityContext.Homeworks.Add(homework);
            entityContext.SaveChanges();
            return true;
        }

        /// <summary>
        /// Gets all homeworks, submitted by a student
        /// </summary>
        /// <param name="studentId">The student id</param>
        /// <returns>The homeworks, submitted by that student</returns>
        public List<Homework> GetHomeworksByStudent(int studentId)
        {
            CheckAuthentication();

            int classId = (from c in entityContext.Classes.Include("Students")
                           where c.Students.Any(s => s.Id == studentId)
                           select c.Id).First();

            return (from c in entityContext.Classes.Include("Subjects")
                            from s in c.Subjects
                            from l in s.Lessons
                            from h in l.Homeworks
                            where c.Id == classId && h.StudentId == studentId
                            select h).ToList();
        }

        /// <summary>
        /// Gets all marks that the student has received
        /// </summary>
        /// <param name="studentId">The student id</param>
        /// <returns>The marks of this student</returns>
        public List<Mark> GetMarksByStudent(int studentId)
        {
            CheckAuthentication();

            return (from h in entityContext.Homeworks.Include("Marks")
                            from m in h.Marks
                            where h.StudentId == studentId
                            select m).ToList();
        }

        /// <summary>
        /// Evaluates a test that the student has submitted
        /// </summary>
        /// <param name="test">The test to evaluate</param>
        /// <param name="studentId">The student</param>
        /// <returns>The score of the student</returns>
        public int EvaluateTest(Test test, int studentId)
        {
            CheckAuthentication();

            int score = 0; 
            foreach(var q in test.Questions)
            {
                if(!(q.Answers.Any(a => a.IsSelected && !a.IsCorrect)))
                {
                    score += q.Answers.Count(a => a.IsSelected && a.IsCorrect);
                }
            }

            if (entityContext.TestScores
                .Any(ts => ts.TestId == test.Id && ts.StudentId == studentId))
            {
                var existing = entityContext.TestScores
                    .Where(ts => ts.TestId == test.Id && ts.StudentId == studentId)
                    .FirstOrDefault();

                existing.Score = score;
            }
            else
            {
                TestScore testScore = new TestScore();
                testScore.StudentId = studentId;
                testScore.TestId = test.Id;
                testScore.Score = score;

                entityContext.TestScores.Add(testScore);
            }

            entityContext.SaveChanges();

            return score;
        }

        /// <summary>
        /// Find all tests for a given student
        /// </summary>
        /// <param name="studentId">The student</param>
        /// <returns>List of all tests for the student</returns>
        public List<TestView> GetTestViewsByStudent(int studentId)
        {
            CheckAuthentication();

            int classId = (from c in entityContext.Classes.Include("Students")
                           where c.Students.Any(s => s.Id == studentId)
                           select c.Id).First();

            var tests = (from c in entityContext.Classes.Include("Subjects")
                         from s in c.Subjects
                         join t in entityContext.Tests on s.Id equals t.SubjectId
                         join scores in entityContext.TestScores
                             on new { StudentId = studentId, TestId = t.Id }
                             equals new { scores.StudentId, scores.TestId } into sc
                         from ts in sc.DefaultIfEmpty()
                         where c.Id == classId 
                         select new TestView()
                                    {
                                        Id = t.Id,
                                        Date = t.Date,
                                        MaxScore = t.MaxScore,
                                        Subject = s.Name,
                                        Title = t.Title,
                                        Score = (ts == null) ? 0 : ts.Score
                                    }).ToList();

            return tests;
        }

        /// <summary>
        /// Gets a test by given ID
        /// </summary>
        /// <param name="testId">The test to search for</param>
        /// <returns>The test with all of its questions</returns>
        public Test GetTest(int testId)
        {
            CheckAuthentication();

            var test = entityContext.Tests.Where(t => t.Id == testId).FirstOrDefault();

            test.Questions = (from q in entityContext.Questions.Include("Answers")
                              where q.TestId == testId
                              select q).ToList();

            return test;
        }
    }
}
