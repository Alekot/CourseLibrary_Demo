using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
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
        [HttpGet("{courseId}")]
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
    }
}
