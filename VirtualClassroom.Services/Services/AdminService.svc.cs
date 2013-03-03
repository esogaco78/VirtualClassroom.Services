﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using VirtualClassroom.Services.Models;
using VirtualClassroom.Services.Views;

namespace VirtualClassroom.Services.Services
{
    /// <summary>
    /// Implementation of the IAdminService
    /// </summary>
    
    //enable sessions
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Single)]
    public class AdminService : IAdminService
    {
        private VirtualClassroomEntities entityContext = new VirtualClassroomEntities();
        private bool isLogged = false;          //stores login information

        /// <summary>
        /// Checks if the client is authenticated
        /// </summary>
        private void CheckAuthentication()
        {
            if(isLogged == false)
            {
                throw new FaultException("Не сте влезли в системата");
            }
        }

        #region Class Management

        /// <summary>
        /// Adds a class to the database
        /// </summary>
        /// <param name="c">The class to add</param>
        public void AddClass(Class c)
        {
            CheckAuthentication();

            if (!entityContext.Classes.Any(cl => cl.Number == c.Number && cl.Letter == c.Letter))
            {
                entityContext.Classes.Add(c);
                entityContext.SaveChanges();
            }
            else
            {
                throw new FaultException("Класът вече съществува");
            }
        }

        /// <summary>
        /// Removes a list of classes from the database
        /// </summary>
        /// <param name="classes"></param>
        public void RemoveClasses(List<Class> classes)
        {
            CheckAuthentication();

            int[] ids = (from c in classes select c.Id).ToArray();

            var entities = (from c in entityContext.Classes
                            where ids.Contains(c.Id)
                            select c).ToList();

            foreach (var entity in entities)
            {
                entityContext.Classes.Remove(entity);
            }

            entityContext.SaveChanges();
        }

        /// <summary>
        /// Gets all classes from the database
        /// </summary>
        /// <returns>The classes</returns>
        public List<Class> GetClasses()
        {
            CheckAuthentication();

            CheckAuthentication();
            return entityContext.Classes.ToList();
        }


        /// <summary>
        /// Adds a list of classes to a subject
        /// </summary>
        /// <param name="subject">The subject</param>
        /// <param name="classes">The classes to add</param>
        public void AddClassesToSubject(Subject subject, List<Class> classes)
        {
            CheckAuthentication();

            var subjectEntity = entityContext.Subjects.Include("Classes")
                .Where(s => s.Id == subject.Id).FirstOrDefault();

            foreach (var c in classes)
            {
                Class entity = new Class() { Id = c.Id };

                if (!subjectEntity.Classes.Any(cl => cl.Id == entity.Id))
                {
                    entityContext.Classes.Attach(entity);
                    subjectEntity.Classes.Add(entity);
                }
            }

            entityContext.SaveChanges();
        }

        #endregion

        #region Student Management

        /// <summary>
        /// Registers a student in the system
        /// </summary>
        /// <param name="student">Student information</param>
        /// <param name="passwordCrypt">Encrypted password</param>
        /// <param name="secret">The key to decrypt with</param>
        public void RegisterStudent(Student student, string passwordCrypt, string secret)
        {
            CheckAuthentication();

            string password = Crypto.DecryptStringAES(passwordCrypt, secret);
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            student.PasswordHash = passwordHash;

            if (IsStudentValid(student))
            {
                entityContext.Students.Add(student);
                entityContext.SaveChanges();
            }
            else
            {
                throw new FaultException("Студентът не е валиден или вече съществува");
            }
        }

        /// <summary>
        /// Removes a list of students from the database
        /// </summary>
        /// <param name="students">The students to remove</param>
        public void RemoveStudents(List<Student> students)
        {
            CheckAuthentication();

            int[] ids = (from s in students select s.Id).ToArray();

            var entities = (from s in entityContext.Students
                            where ids.Contains(s.Id)
                            select s).ToList();

            foreach (var entity in entities)
            {
                entityContext.Students.Remove(entity);
            }

            entityContext.SaveChanges();
        }

        /// <summary>
        /// Gets all students from the system
        /// </summary>
        /// <returns>The students encapsulated in a StudentView list</returns>
        public List<StudentView> GetStudentViews()
        {
            CheckAuthentication();

            return (
                       from s in entityContext.Students
                       join c in entityContext.Classes
                           on s.ClassId equals c.Id
                       select new
                       {
                           Id = s.Id,
                           ClassNumber = c.Number,
                           ClassLetter = c.Letter,
                           EGN = s.EGN,
                           FullName = s.FirstName + " " + s.MiddleName + " " + s.LastName,
                           Username = s.Username
                       })
                                  .AsEnumerable()
                                  .Select(x => new StudentView()
                                  {
                                      Class = string.Format("{0} '{1}'", x.ClassNumber, x.ClassLetter),
                                      EGN = x.EGN,
                                      FullName = x.FullName,
                                      Id = x.Id,
                                      Username = x.Username
                                  }).ToList();
        }

        /// <summary>
        /// Validates student information
        /// </summary>
        /// <param name="student">Student information</param>
        /// <returns>Whether the information is valid</returns>
        private bool IsStudentValid(Student student)
        {
            if (!entityContext.Students.Any(s => s.Username == student.Username
                || s.EGN == student.EGN))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Subject Management


        /// <summary>
        /// Adds a subject to the database
        /// </summary>
        /// <param name="subject">The subject to add</param>
        public void AddSubject(Subject subject)
        {
            CheckAuthentication();

            entityContext.Subjects.Add(subject);
            entityContext.SaveChanges();
        }

        /// <summary>
        /// Removes a list of subjects
        /// </summary>
        /// <param name="subjects">The subjects to remove</param>
        public void RemoveSubjects(List<Subject> subjects)
        {
            CheckAuthentication();

            int[] ids = (from s in subjects select s.Id).ToArray();

            var entities = (from s in entityContext.Subjects
                            where ids.Contains(s.Id)
                            select s).ToList();

            foreach (var entity in entities)
            {
                entityContext.Subjects.Remove(entity);
            }

            entityContext.SaveChanges();
        }

        /// <summary>
        /// Gets all subjects from the database
        /// </summary>
        /// <returns>The subjects, encapsulated in a SubjectView list</returns>
        public List<SubjectView> GetSubjectViews()
        {
            CheckAuthentication();

            return (
                       from s in entityContext.Subjects
                       join t in entityContext.Teachers
                           on s.TeacherId equals t.Id
                       select new SubjectView()
                       {
                           Id = s.Id,
                           Name = s.Name,
                           TeacherFullName = t.FirstName + " " + t.MiddleName + " " + t.LastName
                       }).ToList();
        }

        /// <summary>
        /// Adds a list of subjects to a class
        /// </summary>
        /// <param name="c">The class</param>
        /// <param name="subjects">The subjects to add</param>
        public void AddSubjectsToClass(Class c, List<Subject> subjects)
        {
            CheckAuthentication();

            var classEntity = entityContext.Classes.Include("Subjects")
                .Where(cl => cl.Id == c.Id).FirstOrDefault();

            foreach (var subject in subjects)
            {
                Subject entity = new Subject() { Id = subject.Id };

                if (!classEntity.Subjects.Any(s => s.Id == entity.Id))
                {
                    entityContext.Subjects.Attach(entity);
                    classEntity.Subjects.Add(entity);
                }
            }

            entityContext.SaveChanges();
        }

        /// <summary>
        /// Gets all subjects by a given class
        /// </summary>
        /// <param name="classId">The class to search</param>
        /// <returns>All subjects by that class</returns>
        public List<Subject> GetSubjectsByClass(int classId)
        {
            CheckAuthentication();

            var sub = (from cl in entityContext.Classes.Include("Subjects")
                       where cl.Id == classId
                       select cl.Subjects).First().ToList();

            return sub;
        }

        #endregion

        #region TeacherManagement

        /// <summary>
        /// Registers a teacher in the system
        /// </summary>
        /// <param name="teacher">Teacher information</param>
        /// <param name="passwordCrypt">Encrypted password</param>
        /// <param name="secret">The skey to decrypt with</param>
        public void RegisterTeacher(Teacher teacher, string passwordCrypt, string secret)
        {
            CheckAuthentication();

            string password = Crypto.DecryptStringAES(passwordCrypt, secret);
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            teacher.PasswordHash = passwordHash;

            if (IsTeacherValid(teacher))
            {
                entityContext.Teachers.Add(teacher);
                entityContext.SaveChanges();
            }
            else
            {
                throw new FaultException("Учителят не е валиден или вече съществува");
            }
        }

        /// <summary>
        /// Removes a list of teacher from the system
        /// </summary>
        /// <param name="teachers">The teachers to remove</param>
        public void RemoveTeachers(List<Teacher> teachers)
        {
            CheckAuthentication();

            int[] ids = (from t in teachers select t.Id).ToArray();

            var entities = (from t in entityContext.Teachers
                            where ids.Contains(t.Id)
                            select t).ToList();

            foreach (var entity in entities)
            {
                entityContext.Teachers.Remove(entity);
            }

            entityContext.SaveChanges();
        }

        /// <summary>
        /// Gets all teachers from the system
        /// </summary>
        /// <returns></returns>
        public List<Teacher> GetTeachers()
        {
            CheckAuthentication();

            return entityContext.Teachers.ToList();
        }

        /// <summary>
        /// Validates teacher information
        /// </summary>
        /// <param name="teacher">Teacher information</param>
        /// <returns>Whether the information is valid</returns>
        private bool IsTeacherValid(Teacher teacher)
        {
            if (!entityContext.Teachers.Any(t => t.Username == teacher.Username))
            {
                return true;
            }

            return false;
        }
        #endregion

        #region Admin Management

        /// <summary>
        /// Registers an admin in the system
        /// </summary>
        /// <param name="admin">Admin information</param>
        /// <param name="passwordCrypt">Encrypted password</param>
        /// <param name="secret">The key to decrypt with</param>
        public void RegisterAdmin(Admin admin, string passwordCrypt, string secret)
        {
            CheckAuthentication();

            string password = Crypto.DecryptStringAES(passwordCrypt, secret);
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            admin.PasswordHash = passwordHash;

            if(IsAdminValid(admin))
            {
                entityContext.Admins.Add(admin);
                entityContext.SaveChanges();
            }
            else
            {
                throw new FaultException("Администраторът не е валиден или вече съществува");
            }
        }

        /// <summary>
        /// Logs and admin into the system
        /// </summary>
        /// <param name="usernameCrypt">Encrypted username</param>
        /// <param name="passwordCrypt">Encrypted password</param>
        /// <param name="secret">The key to decrypt with</param>
        /// <returns>Admin information (if successfull login)</returns>
        public Admin LoginAdmin(string usernameCrypt, string passwordCrypt, string secret)
        {
            //empty information
            if(string.IsNullOrWhiteSpace(usernameCrypt) || string.IsNullOrEmpty(usernameCrypt)
                || string.IsNullOrWhiteSpace(passwordCrypt) || string.IsNullOrEmpty(passwordCrypt)
                || string.IsNullOrWhiteSpace(secret) || string.IsNullOrEmpty(secret))
            {
                return null;
            }
        

            //decrypt login details
            string username = Crypto.DecryptStringAES(usernameCrypt, secret);
            string password = Crypto.DecryptStringAES(passwordCrypt, secret);

            if(entityContext.Admins.Count(a => a.Username == username) == 0)
            {
                return null;
            }

            Admin entity = entityContext.Admins.Where(a => a.Username == username).First();
            if (BCrypt.Net.BCrypt.Verify(password, entity.PasswordHash))
            {
                //password is valid
                isLogged = true;
                return entity;
            }

            return null;
        }

        /// <summary>
        /// Validates admin information
        /// </summary>
        /// <param name="admin">Admin information</param>
        /// <returns>Whether the information is valid</returns>
        private bool IsAdminValid(Admin admin)
        {
            if(!entityContext.Admins.Any(a => a.Username == admin.Username))
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}
