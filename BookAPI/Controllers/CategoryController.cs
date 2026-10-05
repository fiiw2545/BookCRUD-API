using Microsoft.AspNetCore.Mvc;
using Npgsql;
using BookAPI.Models;

namespace BookAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        [HttpGet]
        [Route("[action]")]
        public IActionResult ListCategory()
        {
            try
            {
                List<CategoryModel> categories = new List<CategoryModel>();

                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();

                cmd.CommandText = @" SELECT id, name FROM tb_category ORDER BY id ASC ";

                using NpgsqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    categories.Add(new CategoryModel
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Name = reader["name"].ToString()
                    });
                }

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = ex.Message
                    }
                );
            }
        }
    }
}