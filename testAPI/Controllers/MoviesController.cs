using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using testAPI.Dtos;
using testAPI.Models;
using testAPI.Services;

namespace testAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly IMoviesService _moviesService;
        private readonly IGenresService _genresService;
        private readonly IMapper _mapper;
        private readonly List<string> _allowedExtensions = new List<string> { ".jpg", ".png" };
        private const long _maxAllowedPosterSize = 1048576; // 1 MB

        public MoviesController(IMoviesService moviesService, IGenresService genresService, IMapper mapper)
        {
            _moviesService = moviesService;
            _genresService = genresService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllAsync()
        {
            var movies = await _moviesService.GetAll();
            var data = _mapper.Map<IEnumerable<MovieDetailsDto>>(movies);
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetByIdAsync(int id)
        {
            var movie = await _moviesService.GetById(id);
            if (movie == null)
                return NotFound();

            var dto = _mapper.Map<MovieDetailsDto>(movie);
            return Ok(dto);
        }

        [HttpGet("ByGenre/{genreId?}")]
        public async Task<ActionResult> GetByGenreAsync(byte? genreId)
        {
            if (genreId == null)
                return BadRequest("GenreId is required.");

            var movies = await _moviesService.GetAll(genreId.Value);
            var data = _mapper.Map<IEnumerable<MovieDetailsDto>>(movies);
            return Ok(data);
        }

        [HttpPost]
        public async Task<ActionResult> CreateAsync([FromForm] MovieDto dto)
        {
            if (dto.Poster == null)
                return BadRequest("Poster is required.");

            if (!_allowedExtensions.Contains(Path.GetExtension(dto.Poster.FileName).ToLower()))
                return BadRequest("Only .png and .jpg images are allowed.");

            if (dto.Poster.Length > _maxAllowedPosterSize)
                return BadRequest("Max allowed size for poster is 1 MB.");

            if (!dto.GenreId.HasValue)
                return BadRequest("GenreId is required.");

            var isValidGenre = await _genresService.IsvalidGenre(dto.GenreId.Value);
            if (!isValidGenre)
                return BadRequest("Invalid genre.");

            var movie = _mapper.Map<Movie>(dto);

            using var dataStream = new MemoryStream();
            await dto.Poster.CopyToAsync(dataStream);
            movie.Poster = dataStream.ToArray();

            await _moviesService.Add(movie);
            return Ok(movie);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAsync(int id, [FromForm] MovieDto dto)
        {
            var movie = await _moviesService.GetById(id);
            if (movie == null)
                return NotFound($"No movie was found with ID: {id}");

            if (dto.GenreId.HasValue)
            {
                var isValidGenre = await _genresService.IsvalidGenre(dto.GenreId.Value);
                if (!isValidGenre)
                    return BadRequest("Invalid genre.");

                movie.GenreId = dto.GenreId.Value;
            }

            // Update fields only if they are provided in the dto
            if (!string.IsNullOrEmpty(dto.Title))
                movie.Title = dto.Title;

            if (dto.Year.HasValue)
                movie.Year = dto.Year.Value;

            if (!string.IsNullOrEmpty(dto.Storeline))
                movie.Storeline = dto.Storeline;

            if (dto.Rate.HasValue)
                movie.Rate = dto.Rate;

            if (dto.Poster != null)
            {
                if (!_allowedExtensions.Contains(Path.GetExtension(dto.Poster.FileName).ToLower()))
                    return BadRequest("Only .png and .jpg images are allowed.");

                if (dto.Poster.Length > _maxAllowedPosterSize)
                    return BadRequest("Max allowed size for poster is 1 MB.");

                using var dataStream = new MemoryStream();
                await dto.Poster.CopyToAsync(dataStream);
                movie.Poster = dataStream.ToArray();
            }

            _moviesService.Update(movie);
            return Ok(movie);
        }



        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAsync(int id)
        {
            var movie = await _moviesService.GetById(id);
            if (movie == null)
                return NotFound($"No movie was found with ID: {id}");

            _moviesService.Delete(movie);
            return Ok(movie);
        }
    }
}
