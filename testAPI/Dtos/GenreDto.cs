using System.ComponentModel.DataAnnotations;

namespace testAPI.Dtos
{
    public class GenreDto
    {
        [MaxLength(100)]
        public string Name { get; set; }
    }
}
