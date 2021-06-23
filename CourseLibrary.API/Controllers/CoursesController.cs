using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CourseLibrary.API.Controllers
{
    [ApiController]
    [Route("api/authors/{authorId}/courses")]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseLibraryRepository courseLibraryRepository;
        private readonly IMapper mapper;

        public CoursesController(ICourseLibraryRepository courseLibraryRepository, IMapper mapper)
        {
            this.courseLibraryRepository = courseLibraryRepository ??
                throw new ArgumentNullException(nameof(courseLibraryRepository));
            this.mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
        }

        public ActionResult<IEnumerable<CourseDto>> GetCoursesForAuthor(Guid authorId)
        {
            if (!this.courseLibraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var coursesForAuthorsFromRepo = this.courseLibraryRepository.GetCourses(authorId);

            return Ok(this.mapper.Map<IEnumerable<CourseDto>>(coursesForAuthorsFromRepo));
        }
        [HttpGet("{courseId}", Name = "GetCourseForAuthor")]
        public ActionResult<CourseDto> GetCourseFromAuthor(Guid authorId, Guid courseId)
        {
            if (!this.courseLibraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }
            var courseForAuthorFromRepo = this.courseLibraryRepository.GetCourse(authorId, courseId);
            if (courseForAuthorFromRepo == null)
                return NotFound();

            return Ok(this.mapper.Map<CourseDto>(courseForAuthorFromRepo));
        }
        [HttpPost]
        public ActionResult<CourseDto> CreateCourseForAuthor(
            Guid authorId, 
            CourseForCreationDto course)
        {
            if (!this.courseLibraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var courseEntity = this.mapper.Map<Course>(course);
            this.courseLibraryRepository.AddCourse(authorId, courseEntity);
            this.courseLibraryRepository.Save();

            var courseToReturn = this.mapper.Map<CourseDto>(courseEntity);
            return CreatedAtRoute("GetCourseForAuthor",
                        new { authorId, courseId = courseToReturn.Id }, courseToReturn);

        }
        [HttpPut("{courseId}")]
        public IActionResult UpdateCourseForAuthor(Guid authorId,
            Guid courseId, 
            CourseForUpdateDto course)
        {
            if (!this.courseLibraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var courseForAuthorFromRepo = this.courseLibraryRepository.GetCourse(authorId, courseId);

            if (courseForAuthorFromRepo == null)
            {
                //return NotFound(); //commented out to implement upserting
                var courseToAdd = this.mapper.Map<Course>(course);
                courseToAdd.Id = courseId;

                this.courseLibraryRepository.AddCourse(authorId, courseToAdd);
                this.courseLibraryRepository.Save();

                var courseToReturn = this.mapper.Map<CourseDto>(courseToAdd);

                return CreatedAtRoute("GetCourseForAuthor",
                        new { authorId, courseId = courseToReturn.Id },
                        courseToReturn);
            }

            //map the entity to a CourseForUpdateDto
            //apply the updated field values to that dto
            //map the CourseForUpdateDto back to an entity
            this.mapper.Map(course, courseForAuthorFromRepo);

            this.courseLibraryRepository.UpdateCourse(courseForAuthorFromRepo);
            this.courseLibraryRepository.Save();

            //return Ok(courseForAuthorFromRepo); //this returns 200 ok status code, with the modified object
            return NoContent(); //this returns 204 status code
        }
        [HttpPatch("{courseId}")]
        public ActionResult PartiallyUpdateCourseForAuthor(Guid authorId,
            Guid courseId,
            JsonPatchDocument<CourseForUpdateDto> patchDocument)
        {
            if (!this.courseLibraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var courseForAuthorFromRepo = this.courseLibraryRepository.GetCourse(authorId, courseId);

            if (courseForAuthorFromRepo == null)
            {
                //commented out to implement upserting when course does not exist
                //return NotFound();

                var courseDto = new CourseForUpdateDto();
                patchDocument.ApplyTo(courseDto, ModelState);
                if (!TryValidateModel(courseDto))
                {
                    return ValidationProblem(ModelState);
                }

                var courseToAdd = this.mapper.Map<Course>(courseDto);
                courseToAdd.Id = courseId;

                this.courseLibraryRepository.AddCourse(authorId, courseToAdd);
                this.courseLibraryRepository.Save();

                var courseToReturn = this.mapper.Map<CourseDto>(courseToAdd);

                return CreatedAtRoute("GetCourseForAuthor",
                    new { authorId, courseId = courseToReturn.Id },
                    courseToReturn);
            }

            var courseToPatch = this.mapper.Map<CourseForUpdateDto>(courseForAuthorFromRepo);
            //add validation
            patchDocument.ApplyTo(courseToPatch, ModelState);
            if (!TryValidateModel(courseToPatch))
            {
                return ValidationProblem(ModelState);
            }

            //map the entity to a CourseForUpdateDto
            //apply the updated field values to that dto
            //map the CourseForUpdateDto back to an entity
            this.mapper.Map(courseToPatch, courseForAuthorFromRepo);
            this.courseLibraryRepository.UpdateCourse(courseForAuthorFromRepo);
            this.courseLibraryRepository.Save();

            return NoContent();
        }
        public override ActionResult ValidationProblem([ActionResultObjectValue] ModelStateDictionary modelStateDictionary)
        {
            var options = HttpContext.RequestServices
                .GetRequiredService<IOptions<ApiBehaviorOptions>>();

            return (ActionResult)options.Value.InvalidModelStateResponseFactory(ControllerContext);
            //return base.ValidationProblem(modelStateDictionary);
        }
    }
}
