﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using VirtualClassroom.Services.Models;
using VirtualClassroom.Services.Views;

namespace VirtualClassroom.Services.Services
{
    /// <summary>
    /// Interface contract for the teacher service
    /// </summary>
    
    //using reliable sessions
    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface ITeacherService
    {
        [OperationContract]
        Teacher LoginTeacher(string usernameCrypt, string passwordCrypt, string secret);

        [OperationContract]
        void AddLesson(Lesson lesson);

        [OperationContract]
        void RemoveLessons(List<Lesson> lessons);

        [OperationContract]
        List<HomeworkView> GetHomeworkViewsByTeacher(int teacherId);

        [OperationContract]
        List<LessonView> GetLessonViewsByTeacher(int teacherId);

        [OperationContract]
        List<Subject> GetSubjectsByTeacher(int teacherId);

        [OperationContract]
        List<MarkView> GetMarkViewsByTeacher(int teacherId);

        [OperationContract]
        void AddMark(Mark mark);

        [OperationContract]
        File DownloadLessonContent(int lessonId);

        [OperationContract]
        File DownloadLessonHomework(int lessonId);

        [OperationContract]
        File DownloadSubmittedHomework(int homeworkId);

        [OperationContract]
        void AddTest(Test test);

        [OperationContract]
        List<TestView> GetTestsByTeacher(int teacherId);

        [OperationContract]
        Test GetTest(int id);

        [OperationContract]
        void RemoveTests(List<Test> tests);
    }
}
