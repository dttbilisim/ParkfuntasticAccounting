using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Net;
using System.Net.Mail;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Entities.Accounting;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Dtos;
using ecommerce.EP.Services;

namespace ecommerce.EP.Controllers
{
    /// <summary>
    /// Authentication and Authorization Controller
    /// </summary>
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        /// <summary>E-posta doğrulama kodu geçerlilik süresi (kayıt adımları uzun sürebildiği için 20 dk).</summary>
        private static readonly TimeSpan EmailVerificationCodeTtl = TimeSpan.FromMinutes(20);

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<AuthController> _logger;
        private readonly ICourierDocumentUploadService _courierDocUpload;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration, 
            ApplicationDbContext dbContext,
            IRedisCacheService redisCacheService,
            ILogger<AuthController> logger,
            ICourierDocumentUploadService courierDocUpload)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _dbContext = dbContext;
            _redisCacheService = redisCacheService;
            _logger = logger;
            _courierDocUpload = courierDocUpload;
        }

        /// <summary>
        /// Doğrudan SMTP ile doğrulama kodu e-postası gönderir (Hangfire bağımlılığı yok)
        /// </summary>
        private async Task SendVerificationCodeEmail(string fullname, string email, string code)
        {
            var settings = _configuration.GetSection("EmailSetting");
            var smtpHost = settings["SmtpHost"] ?? throw new InvalidOperationException("EmailSetting:SmtpHost yapılandırılmamış");
            var smtpPort = int.Parse(settings["SmtpPort"] ?? "587");
            var smtpSsl = bool.Parse(settings["SmtpSSL"] ?? "true");
            var smtpEmail = settings["SmtpEmail"] ?? throw new InvalidOperationException("EmailSetting:SmtpEmail yapılandırılmamış");
            var smtpPassword = settings["SmtpPassword"] ?? throw new InvalidOperationException("EmailSetting:SmtpPassword yapılandırılmamış");
            var smtpTitle = settings["SmtpTitle"] ?? "ParPazar";

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpEmail, smtpPassword),
                EnableSsl = smtpSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpEmail!, smtpTitle),
                Subject = $"Bicops Doğrulama Kodunuz: {code}",
                IsBodyHtml = true,
                Body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 500px; margin: 0 auto; padding: 0; background: #ffffff;'>
                        <div style='background: linear-gradient(135deg, #7B2FF7, #9B59B6); padding: 32px 20px; text-align: center; border-radius: 0 0 24px 24px;'>
                            <div style='font-size: 36px; font-weight: 800; color: #ffffff; letter-spacing: 2px;'>BICOPS</div>
                            <div style='font-size: 13px; color: rgba(255,255,255,0.8); margin-top: 4px;'>Güvenli Doğrulama</div>
                        </div>
                        <div style='padding: 32px 24px;'>
                            <h2 style='color: #2D2D3F; text-align: center; margin: 0 0 8px 0; font-size: 22px;'>E-posta Doğrulama</h2>
                            <p style='color: #6B7280; text-align: center; margin: 0 0 24px 0; font-size: 14px;'>Merhaba <strong style='color: #2D2D3F;'>{fullname}</strong>, hesabınızı doğrulamak için aşağıdaki kodu kullanın.</p>
                            <div style='background: #F3EAFF; border: 2px solid #D8B4FE; border-radius: 12px; padding: 24px; text-align: center; margin: 0 0 24px 0;'>
                                <span style='font-size: 36px; font-weight: 800; letter-spacing: 10px; color: #7B2FF7;'>{code}</span>
                            </div>
                            <p style='color: #9CA3AF; font-size: 12px; text-align: center; margin: 0;'>Bu kod 10 dakika içinde geçerliliğini yitirecektir.</p>
                        </div>
                        <div style='background: #F9FAFB; padding: 16px 24px; text-align: center; border-top: 1px solid #E5E7EB;'>
                            <p style='color: #9CA3AF; font-size: 11px; margin: 0;'>Bu e-postayı siz talep etmediyseniz lütfen dikkate almayın.</p>
                        </div>
                    </div>"
            };
            mailMessage.To.Add(new MailAddress(email, fullname));

            await client.SendMailAsync(mailMessage);
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        /// <param name="model">Login credentials (Username/Email and Password)</param>
        /// <returns>JWT token and user information</returns>
        /// <response code="200">Returns the JWT token and user details</response>
        /// <response code="401">If credentials are invalid or user is not authorized for this application</response>
        [HttpPost("login")]
        [EnableRateLimiting("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(model.Username);
            }

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                // Allow login for Plasiyer, CustomerB2B, B2C, Customer, Courier or CourierApplicant (pending)
                bool isAuthorized = userRoles.Any(r => r == "Plasiyer" || r == "CustomerB2B" || r == "B2C" || r == "Customer" || r == "Courier" || r == "CourierApplicant");
                
                if (!isAuthorized)
                {
                    // AUDIT LOG: Yetkisiz giriş denemesi
                    _logger.LogWarning(
                        "[AUDIT] Yetkisiz giriş denemesi. UserId: {UserId}, Username: {Username}, IP: {IpAddress}, UserAgent: {UserAgent}",
                        user.Id, user.UserName, ipAddress, userAgent);
                    
                    return Unauthorized(new { message = "Bu uygulama için yetkiniz bulunmamaktadır." });
                }

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName!),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim("FullName", user.FullName ?? ""),
                    new Claim("SalesPersonId", user.SalesPersonId?.ToString() ?? ""),
                    new Claim("CustomerId", user.CustomerId?.ToString() ?? "")
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                // Add BranchId if available (for tenant isolation)
                // For User entity, we might need to check how branch is associated.
                // In ApplicationUser it was explicit. In User it might be different.

                var jwtSettings = _configuration.GetSection("Jwt");
                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "Default_Strong_Key_For_Development_Only_123456"));

                var token = new JwtSecurityToken(
                    issuer: jwtSettings["Issuer"] ?? "ecommerce.EP",
                    audience: jwtSettings["Audience"] ?? "ecommerce.App",
                    expires: DateTime.Now.AddMinutes(double.Parse(jwtSettings["ExpiryInMinutes"] ?? "1440")),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

                // CustomerWorkingType ve TaxNumber'ı Customer tablosundan çek
                int? customerWorkingType = null;
                string? taxNumber = null;
                if (user.CustomerId.HasValue)
                {
                    var customerInfo = await _dbContext.Set<Customer>()
                        .Where(c => c.Id == user.CustomerId.Value)
                        .Select(c => new { WorkingType = (int?)c.CustomerWorkingType, c.TaxNumber })
                        .FirstOrDefaultAsync();
                    customerWorkingType = customerInfo?.WorkingType;
                    taxNumber = customerInfo?.TaxNumber;
                }

                // AUDIT LOG: Başarılı giriş
                _logger.LogInformation(
                    "[AUDIT] Başarılı giriş. UserId: {UserId}, Username: {Username}, Roles: {Roles}, IP: {IpAddress}, UserAgent: {UserAgent}",
                    user.Id, user.UserName, string.Join(",", userRoles), ipAddress, userAgent);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo,
                    user = new {
                        user.Id,
                        user.UserName,
                        user.FullName,
                        Roles = userRoles,
                        CustomerWorkingType = customerWorkingType,
                        TaxNumber = taxNumber,
                        ParentCourierId = user.ParentCourierId
                    }
                });
            }

            // AUDIT LOG: Başarısız giriş denemesi
            _logger.LogWarning(
                "[AUDIT] Başarısız giriş denemesi. Username: {Username}, IP: {IpAddress}, UserAgent: {UserAgent}",
                model.Username, ipAddress, userAgent);

            return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı." });
        }

        /// <summary>
        /// Plasiyer konum bilgisini günceller (Redis'teki online_user_detail kaydına yazar)
        /// </summary>
        /// <param name="model">Latitude ve Longitude koordinatları</param>
        /// <returns>200 OK veya hata</returns>
        /// <response code="200">Konum başarıyla güncellendi</response>
        /// <response code="400">Geçersiz koordinat değerleri</response>
        /// <response code="401">Yetkisiz erişim</response>
        [HttpPost("update-location")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationModel model)
        {
            // Koordinat validasyonu
            if (model.Latitude < -90 || model.Latitude > 90)
            {
                return BadRequest(new { message = "Geçersiz koordinat değerleri: Latitude -90 ile 90 arasında olmalıdır." });
            }

            if (model.Longitude < -180 || model.Longitude > 180)
            {
                return BadRequest(new { message = "Geçersiz koordinat değerleri: Longitude -180 ile 180 arasında olmalıdır." });
            }

            // JWT claim'lerinden userId al
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });
            }

            var userId = userIdClaim.Value;
            var redisKey = $"online_user_detail:{userId}";

            try
            {
                // Redis'ten mevcut kullanıcı detayını oku
                var existingUser = await _redisCacheService.GetAsync<OnlineUserDto>(redisKey);

                if (existingUser != null)
                {
                    // Konum bilgilerini güncelle
                    existingUser.Latitude = model.Latitude;
                    existingUser.Longitude = model.Longitude;
                    existingUser.LastActiveTime = DateTime.UtcNow;

                    // Redis'e geri yaz (TTL: 65 dakika)
                    await _redisCacheService.SetAsync(redisKey, existingUser, TimeSpan.FromMinutes(65));
                }
                else
                {
                    // Kullanıcı kaydı yoksa yeni oluştur (login sonrası ilk konum güncellemesi)
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user == null)
                    {
                        return NotFound(new { message = "Kullanıcı bulunamadı." });
                    }

                    var newUserDetail = new OnlineUserDto
                    {
                        UserId = userId,
                        Username = user.UserName ?? "",
                        FullName = user.FullName ?? "",
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                        LastPageUrl = "",
                        LastActiveTime = DateTime.UtcNow,
                        ConnectionId = "",
                        Application = "Mobile",
                        Latitude = model.Latitude,
                        Longitude = model.Longitude
                    };

                    await _redisCacheService.SetAsync(redisKey, newUserDetail, TimeSpan.FromMinutes(65));
                }

                // online_users sorted set score'unu güncelle (Unix timestamp)
                var sortedSetKey = "online_users";
                var score = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await _redisCacheService.AddToSortedSetAsync(sortedSetKey, userId, score);

                // Konum geçmişini kaydet — günlük Redis list
                // Key formatı: location_history:{userId}:{yyyy-MM-dd}
                var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var historyKey = $"location_history:{userId}:{today}";
                var historyEntry = System.Text.Json.JsonSerializer.Serialize(new
                {
                    lat = model.Latitude,
                    lng = model.Longitude,
                    ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
                await _redisCacheService.ListRightPushAsync(historyKey, historyEntry);
                // TTL: 3 gün (geçmiş verileri otomatik temizlensin)
                await _redisCacheService.SetKeyExpiryAsync(historyKey, TimeSpan.FromDays(3));

                Console.WriteLine($"[KONUM GEÇMİŞİ] userId={userId}, key={historyKey}, entry={historyEntry}");

                return Ok(new { message = "Konum başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                // Hata logla (production'da ILogger kullanılmalı)
                Console.Error.WriteLine($"Konum güncelleme hatası: {ex.Message}");
                return StatusCode(500, new { message = "Konum güncellenirken bir hata oluştu." });
            }
        }

        /// <summary>
        /// Kullanıcının parolasını değiştirir.
        /// Mevcut parola doğrulanır, ardından yeni parola set edilir.
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            if (string.IsNullOrWhiteSpace(model.CurrentPassword) || string.IsNullOrWhiteSpace(model.NewPassword))
            {
                return BadRequest(new { message = "Mevcut parola ve yeni parola zorunludur." });
            }

            if (model.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "Yeni parola en az 6 karakter olmalıdır." });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(new { message = "Kullanıcı bulunamadı." });
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { message = $"Parola değiştirilemedi: {errors}" });
            }

            return Ok(new { message = "Parola başarıyla değiştirildi." });
        }

        /// <summary>
        /// B2C kullanıcı kaydı (Son kullanıcı - Normal müşteri)
        /// </summary>
        [HttpPost("register-b2c")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterB2C([FromBody] RegisterB2CModel model)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            // Validasyon
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new { message = "E-posta ve şifre zorunludur." });
            }

            if (model.Password.Length < 6)
            {
                return BadRequest(new { message = "Şifre en az 6 karakter olmalıdır." });
            }

            if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
            {
                return BadRequest(new { message = "Ad ve soyad zorunludur." });
            }

            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                return BadRequest(new { message = "Telefon numarası zorunludur." });
            }

            // E-posta kontrolü
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                // AUDIT LOG: Duplicate email kayıt denemesi
                _logger.LogWarning(
                    "[AUDIT] Duplicate email ile kayıt denemesi. Email: {Email}, IP: {IpAddress}",
                    model.Email, ipAddress);
                
                return BadRequest(new { message = "Bu e-posta adresi zaten kayıtlı." });
            }

            // Kullanıcı adı kontrolü (email'i kullanıcı adı olarak kullanıyoruz)
            var existingUsername = await _userManager.FindByNameAsync(model.Email);
            if (existingUsername != null)
            {
                return BadRequest(new { message = "Bu kullanıcı adı zaten kayıtlı." });
            }

            // Yeni ApplicationUser oluştur
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                RegisterDate = DateTime.Now,
                EmailConfirmed = false, // E-posta doğrulama gerekirse
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                
                // AUDIT LOG: Kayıt başarısız
                _logger.LogWarning(
                    "[AUDIT] Kayıt başarısız. Email: {Email}, Errors: {Errors}, IP: {IpAddress}",
                    model.Email, errors, ipAddress);
                
                return BadRequest(new { message = $"Kayıt başarısız: {errors}" });
            }

            // B2C rolü yoksa oluştur
            const string roleName = "B2C";
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            }

            // Rolü kullanıcıya ata
            await _userManager.AddToRoleAsync(user, roleName);

            // 6 haneli doğrulama kodu oluştur ve Redis'e kaydet
            var verificationCode = new Random().Next(100000, 999999).ToString();
            var redisKey = $"email_verification:{user.Email}";
            await _redisCacheService.SetAsync(redisKey, verificationCode, EmailVerificationCodeTtl);

            // Doğrulama kodunu email ile gönder
            try
            {
                await SendVerificationCodeEmail(
                    $"{model.FirstName} {model.LastName}",
                    model.Email,
                    verificationCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-posta gönderme hatası: {Email}", model.Email);
            }

            // AUDIT LOG: Başarılı kayıt
            _logger.LogInformation(
                "[AUDIT] Yeni B2C kullanıcı kaydı. UserId: {UserId}, Email: {Email}, IP: {IpAddress}, UserAgent: {UserAgent}",
                user.Id, user.Email, ipAddress, userAgent);

            return Ok(new { 
                message = "Kayıt başarılı. E-posta adresinize gönderilen doğrulama kodunu giriniz.", 
                userId = user.Id,
                email = user.Email,
                requiresVerification = true
            });
        }

        /// <summary>
        /// Kurye kayıt — kullanıcı oluşturur ve otomatik kurye başvurusu açar.
        /// Onay admin panelden yapılır; onaylandığında Courier rolü atanır.
        /// </summary>
        [HttpPost("register-courier")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterCourier([FromBody] RegisterCourierModel model)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                return BadRequest(new { message = "E-posta ve şifre zorunludur." });
            if (model.Password.Length < 6)
                return BadRequest(new { message = "Şifre en az 6 karakter olmalıdır." });
            if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
                return BadRequest(new { message = "Ad ve soyad zorunludur." });
            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
                return BadRequest(new { message = "Telefon numarası zorunludur." });

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
                return BadRequest(new { message = "Bu e-posta adresi zaten kayıtlı." });

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                RegisterDate = DateTime.Now,
                EmailConfirmed = false,
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return BadRequest(new { message = $"Kayıt başarısız: {errors}" });
            }

            // Kurye başvurusu oluştur (Pending)
            var application = new ecommerce.Core.Entities.CourierApplication
            {
                ApplicationUserId = user.Id,
                Phone = model.PhoneNumber.Trim(),
                IdentityNumber = string.IsNullOrWhiteSpace(model.IdentityNumber) ? null : model.IdentityNumber.Trim(),
                Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(),
                Status = ecommerce.Core.Utils.CourierApplicationStatus.Pending,
                AppliedAt = DateTime.UtcNow
            };
            _dbContext.Set<ecommerce.Core.Entities.CourierApplication>().Add(application);
            await _dbContext.SaveChangesAsync();

            // Başvuru sahibi giriş yapabilsin diye CourierApplicant rolü ver (onayda Courier yapılır)
            const string applicantRoleName = "CourierApplicant";
            if (!await _roleManager.RoleExistsAsync(applicantRoleName))
                await _roleManager.CreateAsync(new ApplicationRole { Name = applicantRoleName });
            await _userManager.AddToRoleAsync(user, applicantRoleName);

            // Email doğrulama kodu gönder
            var verificationCode = new Random().Next(100000, 999999).ToString();
            var redisKey = $"email_verification:{user.Email}";
            await _redisCacheService.SetAsync(redisKey, verificationCode, EmailVerificationCodeTtl);
            try
            {
                await SendVerificationCodeEmail($"{model.FirstName} {model.LastName}", model.Email, verificationCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye kayıt e-posta gönderme hatası: {Email}", model.Email);
            }

            _logger.LogInformation("[AUDIT] Yeni kurye kaydı. UserId: {UserId}, Email: {Email}, IP: {IpAddress}", user.Id, user.Email, ipAddress);

            return Ok(new
            {
                message = "Kurye kaydınız alındı. E-posta doğrulamanızı yapın, başvurunuz incelendikten sonra giriş yapabileceksiniz.",
                userId = user.Id,
                email = user.Email,
                requiresVerification = true
            });
        }

        /// <summary>
        /// E-posta kullanılabilir mi kontrolü (kurye kayıt formunda kullanılır — kayıtlı e-posta ile ileri gidilemez).
        /// </summary>
        [HttpGet("check-email-available")]
        [EnableRateLimiting("public")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckEmailAvailable([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { available = false, message = "E-posta adresi girin." });
            var user = await _userManager.FindByEmailAsync(email.Trim());
            return Ok(new { available = user == null });
        }

        /// <summary>
        /// Kurye kayıt (multipart) — belgelerle birlikte. Vergi Levhası, İmza Beyannamesi, Kimlik Fotokopisi (jpg/png/pdf, max 5MB).
        /// </summary>
        [HttpPost("register-courier-form")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [RequestSizeLimit(20_000_000)] // 20 MB total
        public async Task<IActionResult> RegisterCourierForm([FromForm] RegisterCourierFormModel model, CancellationToken ct = default)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                return BadRequest(new { message = "E-posta ve şifre zorunludur." });
            if (model.Password.Length < 6)
                return BadRequest(new { message = "Şifre en az 6 karakter olmalıdır." });
            if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
                return BadRequest(new { message = "Ad ve soyad zorunludur." });
            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
                return BadRequest(new { message = "Telefon numarası zorunludur." });
            if (string.IsNullOrWhiteSpace(model.TaxNumber) || model.TaxNumber.Trim().Length != 10 || !model.TaxNumber.Trim().All(char.IsDigit))
                return BadRequest(new { message = "Vergi numarası 10 haneli olmalıdır." });
            var ibanClean = model.IBAN?.Trim().Replace(" ", "") ?? "";
            if (string.IsNullOrWhiteSpace(ibanClean) || ibanClean.Length != 26 || !ibanClean.StartsWith("TR", StringComparison.OrdinalIgnoreCase) || !ibanClean.Substring(2).All(char.IsDigit))
                return BadRequest(new { message = "Geçerli bir TR IBAN girin (TR + 24 hane)." });
            if (model.TaxPlate == null || model.SignatureDeclaration == null || model.IdCopy == null || model.CriminalRecord == null)
                return BadRequest(new { message = "Tüm belgeler zorunludur: Vergi Levhası, İmza Beyannamesi, Kimlik Fotokopisi ve Sabıka Kaydı." });

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
                return BadRequest(new { message = "Bu e-posta adresi zaten kayıtlı." });

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                RegisterDate = DateTime.Now,
                EmailConfirmed = false,
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return BadRequest(new { message = $"Kayıt başarısız: {errors}" });
            }

            var taxPlatePath = await _courierDocUpload.SaveAsync(model.TaxPlate, ct);
            var signatureDeclarationPath = await _courierDocUpload.SaveAsync(model.SignatureDeclaration, ct);
            var idCopyPath = await _courierDocUpload.SaveAsync(model.IdCopy, ct);
            var criminalRecordPath = await _courierDocUpload.SaveAsync(model.CriminalRecord, ct);

            var application = new ecommerce.Core.Entities.CourierApplication
            {
                ApplicationUserId = user.Id,
                Phone = model.PhoneNumber.Trim(),
                IdentityNumber = string.IsNullOrWhiteSpace(model.IdentityNumber) ? null : model.IdentityNumber.Trim(),
                Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(),
                Status = ecommerce.Core.Utils.CourierApplicationStatus.Pending,
                AppliedAt = DateTime.UtcNow,
                TaxPlatePath = taxPlatePath,
                SignatureDeclarationPath = signatureDeclarationPath,
                IdCopyPath = idCopyPath,
                CriminalRecordPath = criminalRecordPath,
                TaxNumber = model.TaxNumber.Trim(),
                TaxOffice = string.IsNullOrWhiteSpace(model.TaxOffice) ? null : model.TaxOffice.Trim(),
                IBAN = ibanClean,
                CityId = model.CityId,
                TownId = model.TownId,
            };
            _dbContext.Set<ecommerce.Core.Entities.CourierApplication>().Add(application);
            await _dbContext.SaveChangesAsync();

            const string applicantRoleName = "CourierApplicant";
            if (!await _roleManager.RoleExistsAsync(applicantRoleName))
                await _roleManager.CreateAsync(new ApplicationRole { Name = applicantRoleName });
            await _userManager.AddToRoleAsync(user, applicantRoleName);

            var verificationCode = new Random().Next(100000, 999999).ToString();
            var redisKey = $"email_verification:{user.Email}";
            await _redisCacheService.SetAsync(redisKey, verificationCode, EmailVerificationCodeTtl);
            try
            {
                await SendVerificationCodeEmail($"{model.FirstName} {model.LastName}", model.Email, verificationCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye kayıt e-posta gönderme hatası: {Email}", model.Email);
            }

            _logger.LogInformation("[AUDIT] Yeni kurye kaydı (belgeli). UserId: {UserId}, Email: {Email}, IP: {IpAddress}", user.Id, user.Email, ipAddress);

            return Ok(new
            {
                message = "Kurye kaydınız alındı. E-posta doğrulamanızı yapın, başvurunuz incelendikten sonra giriş yapabileceksiniz.",
                userId = user.Id,
                email = user.Email,
                requiresVerification = true
            });
        }

        /// <summary>
        /// E-posta doğrulama kodu kontrolü
        /// </summary>
        [HttpPost("verify-email")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Code))
            {
                return BadRequest(new { message = "E-posta ve doğrulama kodu zorunludur." });
            }

            // Redis'ten kodu kontrol et
            var redisKey = $"email_verification:{model.Email}";
            var savedCode = await _redisCacheService.GetAsync<string>(redisKey);

            if (savedCode == null)
            {
                return BadRequest(new { message = "Doğrulama kodunun süresi dolmuş. Lütfen yeni kod isteyin." });
            }

            if (savedCode != model.Code)
            {
                return BadRequest(new { message = "Doğrulama kodu hatalı." });
            }

            // Kullanıcıyı bul ve EmailConfirmed'ı true yap
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Kullanıcı bulunamadı." });
            }

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            // Redis'ten kodu sil
            await _redisCacheService.RemoveAsync(redisKey);

            return Ok(new { message = "E-posta doğrulandı. Giriş yapabilirsiniz." });
        }

        /// <summary>
        /// Parola sıfırlama kodu iste
        /// </summary>
        [HttpPost("request-password-reset")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                return BadRequest(new { message = "E-posta adresi zorunludur." });
            }

            // Önce veritabanında kullanıcı var mı kontrol et; yoksa e-posta gönderme
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Bu e-posta adresi ile kayıtlı kullanıcı bulunamadı." });
            }

            // 6 haneli kod oluştur ve Redis'e kaydet
            var resetCode = new Random().Next(100000, 999999).ToString();
            var redisKey = $"password_reset:{model.Email}";
            await _redisCacheService.SetAsync(redisKey, resetCode, TimeSpan.FromMinutes(10));

            // E-posta gönder
            try
            {
                await SendVerificationCodeEmail(
                    user.FullName ?? $"{user.FirstName} {user.LastName}",
                    model.Email,
                    resetCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Parola sıfırlama e-postası gönderilemedi: {Email}", model.Email);
            }

            return Ok(new { message = "Eğer bu e-posta kayıtlıysa, sıfırlama kodu gönderildi." });
        }

        /// <summary>
        /// Parola sıfırlama kodunu doğrula
        /// </summary>
        [HttpPost("verify-reset-code")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Code))
            {
                return BadRequest(new { message = "E-posta ve doğrulama kodu zorunludur." });
            }

            // Redis'ten kodu kontrol et
            var redisKey = $"password_reset:{model.Email}";
            var savedCode = await _redisCacheService.GetAsync<string>(redisKey);

            if (savedCode == null)
            {
                return BadRequest(new { message = "Doğrulama kodunun süresi dolmuş. Lütfen yeni kod isteyin." });
            }

            if (savedCode != model.Code)
            {
                return BadRequest(new { message = "Doğrulama kodu hatalı." });
            }

            // Kod doğru — resetToken oluştur (10 dakika geçerli)
            var resetToken = Guid.NewGuid().ToString();
            var tokenKey = $"password_reset_token:{model.Email}";
            await _redisCacheService.SetAsync(tokenKey, resetToken, TimeSpan.FromMinutes(10));

            // Kodu Redis'ten sil (tek kullanımlık)
            await _redisCacheService.RemoveAsync(redisKey);

            return Ok(new { resetToken, message = "Kod doğrulandı. Yeni parolanızı belirleyebilirsiniz." });
        }

        /// <summary>
        /// Yeni parola belirle
        /// </summary>
        [HttpPost("reset-password")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.ResetToken) || string.IsNullOrWhiteSpace(model.NewPassword))
            {
                return BadRequest(new { message = "Tüm alanlar zorunludur." });
            }

            if (model.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "Yeni parola en az 6 karakter olmalıdır." });
            }

            // Token kontrolü
            var tokenKey = $"password_reset_token:{model.Email}";
            var savedToken = await _redisCacheService.GetAsync<string>(tokenKey);

            if (savedToken == null)
            {
                return BadRequest(new { message = "Sıfırlama oturumunun süresi dolmuş. Lütfen tekrar başlayın." });
            }

            if (savedToken != model.ResetToken)
            {
                return BadRequest(new { message = "Geçersiz sıfırlama token'ı." });
            }

            // Kullanıcıyı bul
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Kullanıcı bulunamadı." });
            }

            // Parolayı sıfırla (eski parola gerekmeden)
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { message = $"Parola sıfırlanamadı: {errors}" });
            }

            // Token'ı Redis'ten sil (tek kullanımlık)
            await _redisCacheService.RemoveAsync(tokenKey);

            // AUDIT LOG: Parola sıfırlama
            _logger.LogInformation(
                "[AUDIT] Parola sıfırlama başarılı. UserId: {UserId}, Email: {Email}",
                user.Id, user.Email);

            return Ok(new { message = "Parolanız başarıyla değiştirildi. Giriş yapabilirsiniz." });
        }

        /// <summary>
        /// Doğrulama kodunu yeniden gönder
        /// </summary>
        [HttpPost("resend-verification")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                return BadRequest(new { message = "E-posta adresi zorunludur." });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Kullanıcı bulunamadı." });
            }

            if (user.EmailConfirmed)
            {
                return BadRequest(new { message = "E-posta zaten doğrulanmış." });
            }

            // Yeni kod oluştur
            var verificationCode = new Random().Next(100000, 999999).ToString();
            var redisKey = $"email_verification:{model.Email}";
            await _redisCacheService.SetAsync(redisKey, verificationCode, EmailVerificationCodeTtl);

            try
            {
                await SendVerificationCodeEmail(
                    user.FullName ?? $"{user.FirstName} {user.LastName}",
                    model.Email,
                    verificationCode);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"E-posta gönderme hatası: {ex.Message}");
            }

            return Ok(new { message = "Doğrulama kodu tekrar gönderildi." });
        }
    }

    public class LoginModel
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class RegisterB2CModel
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
    }

    public class UpdateLocationModel
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class ChangePasswordModel
    {
        public string CurrentPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

    public class VerifyEmailModel
    {
        public string Email { get; set; } = null!;
        public string Code { get; set; } = null!;
    }

    public class ResendVerificationModel
    {
        public string Email { get; set; } = null!;
    }

    public class RequestPasswordResetModel
    {
        public string Email { get; set; } = null!;
    }

    public class VerifyResetCodeModel
    {
        public string Email { get; set; } = null!;
        public string Code { get; set; } = null!;
    }

    public class ResetPasswordModel
    {
        public string Email { get; set; } = null!;
        public string ResetToken { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

    public class RegisterCourierModel
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string? IdentityNumber { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// Multipart form model for courier registration with document uploads.
    /// </summary>
    public class RegisterCourierFormModel
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string? IdentityNumber { get; set; }
        public string? Note { get; set; }
        public string? TaxNumber { get; set; }
        public string? TaxOffice { get; set; }
        public string? IBAN { get; set; }
        public int? CityId { get; set; }
        public int? TownId { get; set; }
        public IFormFile? TaxPlate { get; set; }
        public IFormFile? SignatureDeclaration { get; set; }
        public IFormFile? IdCopy { get; set; }
        public IFormFile? CriminalRecord { get; set; }
    }
}
