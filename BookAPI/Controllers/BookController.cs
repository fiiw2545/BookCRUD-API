using BookAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Cryptography;

namespace BookAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookController : ControllerBase
    {
        [HttpGet]
        [Route("[action]")]
        public IActionResult ListBook()
        {
            try
            {
                List<object> books = new List<object>();
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT
                                    b.id,
                                    b.isbn,
                                    b.name,
                                    b.price,
                                    b.category_id,
                                    c.name AS category_name
                                    FROM tb_book b
                                    INNER JOIN tb_category c
                                    ON b.category_id = c.id
                                    ORDER BY b.id ASC
                                   ";

                using NpgsqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    books.Add(new
                    {
                        id = Convert.ToInt32(reader["id"]),
                        isbn = reader["isbn"].ToString(),
                        name = reader["name"].ToString(),
                        price = Convert.ToInt32(reader["price"]),
                        categoryId = Convert.ToInt32(reader["category_id"]),
                        categoryName = reader["category_name"].ToString()
                    });
                }
                return Ok(books);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = ex.Message,
                });
            }
        }

        [HttpPost]
        [Route("[action]")]
        [Authorize(Roles = "ADMIN")]
        public IActionResult AddBook(BookModel book)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO tb_book (isbn,name,price,category_id) VALUES (@isbn,@name,@price,@category_id)";
                cmd.Parameters.AddWithValue("isbn", book.Isbn!);
                cmd.Parameters.AddWithValue("name", book.Name!);
                cmd.Parameters.AddWithValue("price", book.Price);
                cmd.Parameters.AddWithValue("category_id", book.CategoryId);

                cmd.ExecuteNonQuery();
                return Ok(new
                {
                    message = "บันทึกข้อมูลสำเร็จ"
                });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPut]
        [Route("[action]")]
        [Authorize(Roles = "ADMIN")]
        public IActionResult EditBook(BookModel book)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = @"UPDATE tb_book 
                                    SET isbn = @isbn,
                                        name = @name,
                                        price = @price,
                                        category_id = @category_id
                                    WHERE id = @id
                                    ";
                cmd.Parameters.AddWithValue("id", book.Id);
                cmd.Parameters.AddWithValue("isbn", book.Isbn!);
                cmd.Parameters.AddWithValue("name", book.Name!);
                cmd.Parameters.AddWithValue("price", book.Price);
                cmd.Parameters.AddWithValue("category_id",book.CategoryId);
                cmd.ExecuteNonQuery();

                return Ok(new { message = "แก้ไขข้อมูลสินค้าสำเร็จ" });
            }

            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpDelete]
        [Route("[action]/{id}")]
        [Authorize(Roles = "ADMIN")]
        public IActionResult DeleteBook(int id)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = @"DELETE FROM tb_book WHERE id = @id ";
                cmd.Parameters.AddWithValue("id", id);
                cmd.ExecuteNonQuery();

                return Ok(new
                {
                    message = "ลบข้อมูลสำเร็จ"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("[action]/{id}")]
        public IActionResult GetBook(int id)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT
                                    b.id,b.isbn,b.name,b.price,b.category_id,
                                    c.name AS category_name
                                    FROM tb_book b
                                    INNER JOIN tb_category c ON b.category_id = c.id
                                    WHERE b.id = @id
                                   ";
                cmd.Parameters.AddWithValue("id", id);
                using NpgsqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return Ok(new
                    {
                        id = Convert.ToInt32(reader["id"]),
                        isbn = reader["isbn"].ToString(),
                        name = reader["name"].ToString(),
                        price = Convert.ToInt32(reader["price"]),
                        categoryId = Convert.ToInt32(reader["category_id"]),
                        categoryName = reader["category_name"].ToString()
                    });
                }

                return NotFound(new
                {
                    message = "ไม่พบข้อมูลหนังสือ"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = ex.Message
                });
            }
        }
    }
}
