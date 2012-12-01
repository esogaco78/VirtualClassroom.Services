﻿using System;
using System.Collections.Generic;
using System.Data.Objects.DataClasses;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace VirtualClassroom.Services.POCO_Classes
{
    [DataContract]
    public class Homework
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public byte[] Content { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public Student Student { get; set; }

        [DataMember]
        public Lesson Lesson { get; set; }

        [DataMember]
        public Mark Mark { get; set; }

        public Homework(int id, byte[] content, DateTime date, Student student,
            Lesson lesson, Mark mark)
        {
            this.Id = id;
            this.Content = content;
            this.Date = date;
            this.Student = student;
            this.Lesson = lesson;
            this.Mark = mark;
        }

        public static explicit operator Homework(HomeworkEntity entity)
        {
            Mark mark = null;
            if (entity.Marks != null)
            {
                if (entity.Marks.Count > 0)
                {
                    mark = (Mark)entity.Marks.First();
                }
            }

            Homework homework = new Homework(
                entity.Id,
                entity.Content,
                entity.Date,
                (Student)entity.Student,
                (Lesson)entity.Lesson,
                mark
            );

            return homework;
        }

        public static explicit operator HomeworkEntity(Homework homework)
        {
            HomeworkEntity entity = new HomeworkEntity();
            entity.Id = homework.Id;
            entity.Content = homework.Content;
            entity.Date = homework.Date;
            entity.Student = (StudentEntity)homework.Student;
            entity.Lesson = (LessonEntity)homework.Lesson;
            entity.Marks = new EntityCollection<MarkEntity>();
            if (homework.Mark != null)
            {
                entity.Marks.Add((MarkEntity)homework.Mark);
            }

            return entity;
        }
    }
}
