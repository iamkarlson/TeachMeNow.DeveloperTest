﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.ModelBinding;

using TeachMeNow.DeveloperTest.BackEnd.Models;

namespace TeachMeNow.DeveloperTest.BackEnd.Controllers {
    /// <summary>
    /// </summary>
    /// <seealso cref="System.Web.Http.ApiController" />
    public class ClassesController: BaseApiController {
        private IQueryable<Class> currentUserClasses {
            get {
                var classes = db.Classes.AsQueryable();
                if(currentUser.IsTutor) {
                    classes = classes.Where(t => t.TutorId == currentUser.Id);
                } else {
                    classes = classes.Where(t => t.StudentId == currentUser.Id);
                }
                return classes;
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="ClassesController" /> class.
        /// </summary>
        /// <param name="database">The database.</param>
        public ClassesController(IBackEndDb database): base(database) {
        }

        /// <summary>
        /// Gets all the classes.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IHttpActionResult Get() {
            IQueryable<Class> classes = currentUserClasses;

            return Ok(classes.Select(t => new ClassViewModel(db, t)).AsEnumerable());
        }

        /// <summary>
        /// Gets the specified class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IHttpActionResult Get(int id) {
            IQueryable<Class> classes = currentUserClasses;

            var cl = classes.SingleOrDefault(t => t.Id == id);

            if(cl == default(ClassViewModel)) {
                return NotFound();
            }

            return Ok(new ClassViewModel(db, cl));
        }

        /// <summary>
        /// Adds the Class to the database.
        /// </summary>
        /// <param name="newClass">The new class.</param>
        public IHttpActionResult Post([FromBody] Class newClass) {
            if(newClass == null) {
                ModelState.Clear();
                addModelError(nameof(Class), "Cannot create empty class");
                return BadRequest(ModelState);
            }
            var tutorBusy = db.Classes.Any(t => t.TutorId == newClass.TutorId &&
                                                (
                                                    (t.StartTime > newClass.StartTime && t.StartTime < newClass.EndTime) ||
                                                    (t.EndTime > newClass.StartTime && t.EndTime < newClass.EndTime)
                                                ));

            if(tutorBusy) {
                addModelError(nameof(newClass.StartTime), "Cannot book a class, tutor is busy");
            }

            var studentBusy = db.Classes.Any(t => t.StudentId == newClass.StudentId &&
                                                  (
                                                      (t.StartTime > newClass.StartTime && t.StartTime < newClass.EndTime) ||
                                                      (t.EndTime > newClass.StartTime && t.EndTime < newClass.EndTime)
                                                  ));
            if(studentBusy) {
                addModelError(nameof(newClass.StartTime), "Cannot book a class, student is busy");
            }
            if(currentUser.IsTutor && newClass.TutorId > 0 && newClass.TutorId != currentUser.Id) {
                addModelError(nameof(newClass.TutorId), "Cannot book a class for different user");
            }
            if(!currentUser.IsTutor && newClass.StudentId > 0 && newClass.StudentId != currentUser.Id) {
                addModelError(nameof(newClass.StudentId), "Cannot book a class for different user");
            }

            if(currentUser.IsTutor) {
                if(!db.Users.Any(t => !t.IsTutor && t.Id == newClass.StudentId)) {
                    addModelError(nameof(newClass.StudentId), "Cannot book a class: student doesn't exists");
                }
                newClass.TutorId = currentUser.Id;
                removePropertyError(nameof(Class.TutorId), nameof(newClass));
            } else {
                if(!db.Users.Any(t => t.IsTutor && t.Id == newClass.TutorId)) {
                    addModelError(nameof(newClass.StudentId), "Cannot book a class: tutor doesn't exists");
                }
                newClass.StudentId = currentUser.Id;
                removePropertyError(nameof(Class.StudentId), nameof(newClass));
            }

            if(!ModelState.IsValid) {
                return BadRequest(ModelState);
            }
            db.Classes.Insert(newClass);
            return CreatedAtRoute("DefaultApi", new {
                id = newClass.Id
            }, newClass);
        }

        private void removePropertyError(string name, string root) {
            var errors = ModelState[root]?.Errors;
            if(errors == null) {
                return;
            }
            var errorsRoRemove = new List<ModelError>();
            foreach(ModelError error in errors) {
                if(error.ErrorMessage.Contains(name) || (error.Exception?.Message.Contains(name) ?? false)) {
                    errorsRoRemove.Add(error);
                }
            }
            foreach(ModelError error in errorsRoRemove) {
                errors.Remove(error);
            }
        }

        /// <summary>
        /// Updates the specified Class in the database.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="model">The update class.</param>
        public IHttpActionResult Put(int id, [FromBody] Class model) {
            var dbM = db.Classes.SingleOrDefault(t => t.Id == id);
            if(dbM == null) {
                return NotFound();
            }
            if(model.EndTime != default(DateTime)) {
                dbM.EndTime = model.EndTime;
            }
            if(model.StartTime != default(DateTime)) {
                dbM.StartTime = model.StartTime;
            }
            
            if(!string.IsNullOrWhiteSpace(model.Subject)) {
                dbM.Subject = model.Subject;
            }

            if(model.StudentId != default(int)) {
                dbM.StudentId = model.StudentId;
            }
            
            if(model.TutorId != default(int)) {
                dbM.TutorId = model.TutorId;
            }
            

            db.Classes.Update(dbM);

            return Ok();
        }

        /// <summary>
        /// Deletes the specified Class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public IHttpActionResult Delete(int id) {
            var cl = db.Classes.SingleOrDefault(t => t.Id == id);
            if(cl == null) {
                return NotFound();
            }
            db.Classes.Delete(cl);
            return Ok();
        }
    }
}
