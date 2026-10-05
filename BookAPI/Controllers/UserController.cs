using Microsoft.AspNetCore.Mvc;
using Npgsql;
using BookAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using BCrypt.Net;

namespace BookAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public UserController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        [Route("[action]")]
        public IActionResult Login(LoginModel user)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();

                cmd.CommandText = @"SELECT id, usr, pwd, name, level
                                    FROM tb_user
                                    WHERE usr = @usr
                                  ";

                cmd.Parameters.AddWithValue("usr", user.Usr!);

                using NpgsqlDataReader reader = cmd.ExecuteReader();

                // 1. ตรวจสอบว่ามี Username หรือไม่
                if (!reader.Read())
                {
                    return Unauthorized(new
                    {
                        message = "ไม่พบ Username นี้ในระบบ"
                    });
                }

                // 2. ตรวจสอบ Password
                string dbPassword = reader["pwd"].ToString()!;

                bool passwordValid = BCrypt.Net.BCrypt.Verify(user.Pwd, dbPassword);

                if (!passwordValid)
                {
                    return Unauthorized(new
                    {
                        message = "Password ไม่ถูกต้อง"
                    });
                }

                // 3. Login สำเร็จ
                UserModel loginUser = new UserModel
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Usr = reader["usr"].ToString(),
                    Name = reader["name"].ToString(),
                    Level = reader["level"].ToString()
                };

                string token = CreateToken(loginUser);

                return Ok(new
                {
                    token = token,

                    user = new
                    {
                        id = loginUser.Id,
                        usr = loginUser.Usr,
                        name = loginUser.Name,
                        level = loginUser.Level,
                    },
                    message = "เข้าสู่ระบบสำเร็จ"
                });
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

        [HttpPost]
        [Route("[action]")]
        public IActionResult Register(RegisterModel user)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                // Check username
                cmd.CommandText = @"SELECT COUNT(*) FROM tb_user WHERE usr = @usr ";

                cmd.Parameters.AddWithValue("usr", user.Usr!);

                int count = Convert.ToInt32(cmd.ExecuteScalar());

                if (count > 0)
                {
                    return Conflict(new
                    {
                        message = "Username นี้มีอยู่แล้ว"
                    });
                }
                cmd.Parameters.Clear();
                cmd.CommandText = @"INSERT INTO tb_user(usr,pwd,name,level) VALUES (@usr,@pwd,@name,@level)";
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Pwd);
                cmd.Parameters.AddWithValue("usr", user.Usr!);
                cmd.Parameters.AddWithValue("pwd", hashedPassword);
                cmd.Parameters.AddWithValue("name", user.Name!);
                cmd.Parameters.AddWithValue("level", "USER");
                cmd.ExecuteNonQuery();

                return Ok(new
                {
                    message = "สมัครสมาชิกสำเร็จ"
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

        private string CreateToken(UserModel user)
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Usr ?? ""),
            new Claim("name", user.Name ?? ""),
            new Claim(ClaimTypes.Role, user.Level ?? "")
        };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"]!
                )
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(
                        _configuration["Jwt:ExpireMinutes"]
                    )
                ),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        [HttpGet]
        [Route("[action]")]
        [Authorize(Roles = "ADMIN")]
        public IActionResult AdminOnly()
        {
            return Ok(new
            {
                message = "คุณสามารถเข้าถึง Admin API ได้",
                user = User.Identity?.Name,
                role = User.FindFirst(ClaimTypes.Role)?.Value
            });
        }

        [HttpGet]
        [Route("[action]")]
        [Authorize(Roles = "ADMIN")]
        public IActionResult ListUser()
        {
            try
            {
                List<UserListModel> users = new List<UserListModel>();
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT id,usr,name,level FROM tb_user ORDER BY id ASC";
                using NpgsqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    users.Add(new UserListModel
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Usr = reader["usr"].ToString(),
                        Name = reader["name"].ToString(),
                        Level = reader["level"].ToString()
                    });
                }
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        [Route("[action]")]
        [Authorize(Roles = "ADMIN")]
        public IActionResult CreateUser(CreateUserModel createUserModel)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();

                // ตรวจสอบ Username ซ้ำ
                cmd.CommandText = @" SELECT id FROM tb_user WHERE usr = @usr ";

                cmd.Parameters.AddWithValue("usr", createUserModel.Usr);

                object? result = cmd.ExecuteScalar();

                if (result != null)
                {
                    return Conflict(new
                    {
                        message = "Username นี้มีอยู่แล้ว"
                    });
                }

                cmd.CommandText = @"INSERT INTO tb_user (usr,pwd,name,level) VALUES (@usr,@pwd,@name,@level)";
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(createUserModel.Pwd);
                cmd.Parameters.AddWithValue("usr", createUserModel.Usr!);
                cmd.Parameters.AddWithValue("pwd", passwordHash);
                cmd.Parameters.AddWithValue("name", createUserModel.Name);
                cmd.Parameters.AddWithValue("level", createUserModel.Level);
                cmd.ExecuteNonQuery();
                return Ok(new
                {
                    message = "สมัครสมาชิกสำเร็จ"
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
        [Authorize(Roles = "ADMIN")]
        public IActionResult GetUser(int id)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();

                cmd.CommandText = @" SELECT id,usr,name,level FROM tb_user WHERE id = @id ";

                cmd.Parameters.AddWithValue("id", id);

                using NpgsqlDataReader reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    return NotFound(new
                    {
                        message = "ไม่พบ User นี้"
                    });
                }

                UserListModel user = new UserListModel
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Usr = reader["usr"].ToString(),
                    Name = reader["name"].ToString(),
                    Level = reader["level"].ToString()
                };

                return Ok(user);
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

        [HttpPut]
        [Route("[action]/{id}")]
        [Authorize(Roles = "ADMIN")]
        public IActionResult UpdateUser(int id, UpdateUserModel updateUserModel)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();

                // ตรวจสอบว่า User มีอยู่หรือไม่
                cmd.CommandText = @" SELECT id FROM tb_user WHERE id = @id ";

                cmd.Parameters.AddWithValue("id", id);

                object? userResult = cmd.ExecuteScalar();

                if (userResult == null)
                {
                    return NotFound(new
                    {
                        message = "ไม่พบ User นี้"
                    });
                }

                // ตรวจสอบกรณี ADMIN คนสุดท้าย
                cmd.CommandText = @" SELECT level FROM tb_user WHERE id = @id ";

                cmd.Parameters.Clear();

                cmd.Parameters.AddWithValue("id", id);

                object? currentLevel = cmd.ExecuteScalar();

                if (
                    currentLevel?.ToString() == "ADMIN" && updateUserModel.Level == "USER" 
                    )
                {
                    cmd.CommandText = @" SELECT COUNT(*) FROM tb_user WHERE level = 'ADMIN' ";

                    cmd.Parameters.Clear();

                    int adminCount = Convert.ToInt32(cmd.ExecuteScalar());

                    if (adminCount <= 1)
                    {
                        return Conflict(new
                        {
                            message = "ไม่สามารถลดสิทธิ์ของ ADMIN คนสุดท้ายได้"
                        });
                    }
                }

                // ตรวจสอบ Username ซ้ำ
                cmd.CommandText = @" SELECT id FROM tb_user WHERE usr = @usr AND id <> @id ";

                cmd.Parameters.Clear();

                cmd.Parameters.AddWithValue("usr", updateUserModel.Usr);
                cmd.Parameters.AddWithValue("id", id);

                object? duplicateResult = cmd.ExecuteScalar();

                if (duplicateResult != null)
                {
                    return Conflict(new
                    {
                        message = "Username นี้มีอยู่แล้ว"
                    });
                }

                // Update User
                cmd.CommandText = @" UPDATE tb_user SET usr = @usr, name = @name, level = @level WHERE id = @id ";

                cmd.Parameters.Clear();

                cmd.Parameters.AddWithValue("usr", updateUserModel.Usr);
                cmd.Parameters.AddWithValue("name", updateUserModel.Name);
                cmd.Parameters.AddWithValue("level", updateUserModel.Level);
                cmd.Parameters.AddWithValue("id", id);

                cmd.ExecuteNonQuery();

                return Ok(new
                {
                    message = "แก้ไข User สำเร็จ"
                });
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

        [HttpDelete]
        [Route("[action]/{id}")]
        [Authorize(Roles = "ADMIN")]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();

                // ตรวจสอบว่า User มีอยู่หรือไม่
                cmd.CommandText = @" SELECT id FROM tb_user WHERE id = @id ";

                cmd.Parameters.AddWithValue("id", id);

                object? result = cmd.ExecuteScalar();

                if (result == null)
                {
                    return NotFound(new
                    {
                        message = "ไม่พบ User นี้"
                    });
                }

                // Delete User
                cmd.CommandText = @" DELETE FROM tb_user WHERE id = @id ";

                cmd.Parameters.Clear();

                cmd.Parameters.AddWithValue("id", id);

                cmd.ExecuteNonQuery();

                return Ok(new
                {
                    message = "ลบ User สำเร็จ"
                });
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

