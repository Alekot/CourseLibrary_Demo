using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CourseLibrary.API.Controllers
{
    [ApiController]
    [Route("api/authors")]
    public class AuthorsController : ControllerBase
    {
        private readonly ICourseLibraryRepository _courseLibraryRepository;
        private readonly IMapper _mapper;

        public AuthorsController(ICourseLibraryRepository courseLibraryRepository, IMapper mapper)
        {
            _courseLibraryRepository = courseLibraryRepository ??
                throw new ArgumentNullException(nameof(courseLibraryRepository));
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet(Name = "GetAuthors")]
        [HttpHead]
        public ActionResult<IEnumerable<AuthorDto>> GetAuthors(
            //[FromQuery(Name = "mainCategory")] string mainCategory,
            //string searchQuery
            [FromQuery] AuthorsResourceParameters authorsResourceParameters)
        {
            var authorsFromRepo = _courseLibraryRepository.GetAuthors(authorsResourceParameters);

            var previousPageLink = authorsFromRepo.HasPrevious ?
                CreateAuthorsUri(authorsResourceParameters,
                ResourceUriType.PreviousPage) : null;

            var nextPageLink = authorsFromRepo.HasNext ?
                CreateAuthorsUri(authorsResourceParameters,
                ResourceUriType.NextPage) : null;

            var paginationMetadata = new
            {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                curentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages,
                previousPageLink,
                nextPageLink
            };

            Response.Headers.Add("X-Pagination",
                JsonSerializer.Serialize(paginationMetadata));

            #region obsolete
            //before implementing AutoMapper
            //var authors = new List<AuthorDto>();
            //foreach(var author in authorsFromRepo)
            //{
            //    authors.Add(new AuthorDto
            //    {
            //        Id = author.Id,
            //        Name = $"{author.FirstName} {author.LastName}",
            //        MainCategory = author.MainCategory,
            //        Age = author.DateOfBirth.GetCurrentAge()
            //    });
            //}
            #endregion

            return Ok(_mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo));
        }

        [HttpGet("{authorId}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid authorId)
        {
            var authorFromRepo = _courseLibraryRepository.GetAuthor(authorId);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<AuthorDto>(authorFromRepo));
        }

        [HttpPost]
        public ActionResult<AuthorDto> CreateAuthor(AuthorForCreationDto author)
        {
            // we dont need this check because the [ApiController] atribute is checking whether the request was good and returns 404 if not
            //if (author == null)
            //{
            //    return BadRequest();
            //}

            var authorEntity = _mapper.Map<Author>(author);
            _courseLibraryRepository.AddAuthor(authorEntity);
            _courseLibraryRepository.Save();

            var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);
            return CreatedAtRoute("GetAuthor",
                new { authorId = authorToReturn.Id },
                authorToReturn);
        }

        [HttpOptions]
        public IActionResult getAuthorsOptions()
        {
            Response.Headers.Add("Allow", "GET,OPTIONS,POST");
            return Ok();
        }

        [HttpDelete("{authorId}")]
        public ActionResult DeleteAuthor(Guid authorId)
        {
            var authorFromRepo = _courseLibraryRepository.GetAuthor(authorId);
            if(authorFromRepo == null)
            {
                return NotFound();
            }

            _courseLibraryRepository.DeleteAuthor(authorFromRepo);
            _courseLibraryRepository.Save();

            return NoContent();
        }
        
        private string CreateAuthorsUri(
            AuthorsResourceParameters authorsResourceParameters,
            ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return Url.Link("GetAuthors",
                      new
                      {
                          //fields = authorsResourceParameters.Fields,
                          //orderBy = authorsResourceParameters.OrderBy,
                          pageNumber = authorsResourceParameters.PageNumber - 1,
                          pageSize = authorsResourceParameters.PageSize,
                          mainCategory = authorsResourceParameters.MainCategory,
                          searchQuery = authorsResourceParameters.SearchQuery
                      });
                case ResourceUriType.NextPage:
                    return Url.Link("GetAuthors",
                      new
                      {
                          //fields = authorsResourceParameters.Fields,
                          //orderBy = authorsResourceParameters.OrderBy,
                          pageNumber = authorsResourceParameters.PageNumber + 1,
                          pageSize = authorsResourceParameters.PageSize,
                          mainCategory = authorsResourceParameters.MainCategory,
                          searchQuery = authorsResourceParameters.SearchQuery
                      });
                //case ResourceUriType.Current:
                default:
                    return Url.Link("GetAuthors",
                    new
                    {
                        //fields = authorsResourceParameters.Fields,
                        //orderBy = authorsResourceParameters.OrderBy,
                        pageNumber = authorsResourceParameters.PageNumber,
                        pageSize = authorsResourceParameters.PageSize,
                        mainCategory = authorsResourceParameters.MainCategory,
                        searchQuery = authorsResourceParameters.SearchQuery
                    });
            }

        }
    }
}
