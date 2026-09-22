using Microsoft.EntityFrameworkCore;
using ServiceUsers.Data;
using ServiceUsers.Data.Entities;
using ServiceUsers.Infrastructure;
using StudentPass.Contracts;
using StudentPass.Contracts.Messages;

namespace ServiceUsers.Services;

public sealed class AuthService
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenService _jwt;
    private readonly EmailQueuePublisher _emailQueue;
    private readonly IConfiguration _configuration;

    public AuthService(
        AppDbContext db,
        PasswordHasher passwordHasher,
        JwtTokenService jwt,
        EmailQueuePublisher emailQueue,
        IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _emailQueue = emailQueue;
        _configuration = configuration;
    }

    public async Task SendCodeAsync(string email, CancellationToken cancellationToken)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (existing is not null)
        {
            if (!existing.IsActive)
            {
                throw new HttpDetailException(400, "Этот email был удалён. Восстановление невозможно, зарегистрируйтесь с другим email");
            }

            throw new HttpDetailException(400, "Пользователь с таким email уже существует");
        }

        var code = Random.Shared.Next(100000, 999999).ToString();
        _db.EmailVerifications.Add(new EmailVerification
        {
            UserEmail = email,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        _emailQueue.Publish(new EmailMessage
        {
            Email = email,
            Subject = "Добро пожаловать в StudentPass!",
            Message = $"{code} - Ваш код для регистрации на платформе студенческих скидок StudentPass"
        });
    }

    public async Task<string> RegisterAsync(UserRegister data, CancellationToken cancellationToken)
    {
        // Временно отключена проверка кода с почты. Чтобы вернуть, раскомментируйте блок ниже.
        // var verification = await _db.EmailVerifications.FirstOrDefaultAsync(
        //     x => x.UserEmail == data.Email && x.Code == data.Code.ToString() && x.ExpiresAt > DateTime.UtcNow,
        //     cancellationToken);
        //
        // if (verification is null)
        // {
        //     return "Код не подходит! Попробуйте еще раз";
        // }

        var existing = await _db.Users.FirstOrDefaultAsync(x => x.Email == data.Email, cancellationToken);
        if (existing is not null)
        {
            if (!existing.IsActive)
            {
                throw new HttpDetailException(400, "Этот email был удалён. Восстановление невозможно, зарегистрируйтесь с другим email");
            }

            throw new HttpDetailException(400, "Пользователь с таким email уже существует");
        }

        var adminEmail = (_configuration["AdminBootstrapEmail"] ?? "alex.arkhangelskiy@yandex.ru").Trim();
        var role = string.Equals(data.Email, adminEmail, StringComparison.OrdinalIgnoreCase)
            ? UserRole.Admin
            : UserRole.User;

        _db.Users.Add(new User
        {
            Email = data.Email,
            PasswordHash = _passwordHasher.Hash(data.Password),
            FullName = data.FullName,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _db.EmailVerifications.RemoveRange(_db.EmailVerifications.Where(x => x.UserEmail == data.Email));
        await _db.SaveChangesAsync(cancellationToken);
        return "Вы успешно зарегистрировались!";
    }

    public async Task RegisterPartnerAsync(UserRegisterPartner data, CancellationToken cancellationToken)
    {
        var existingUser = await _db.Users.FirstOrDefaultAsync(x => x.Email == data.Email, cancellationToken);
        if (existingUser is not null && !existingUser.IsActive)
        {
            throw new HttpDetailException(400, "Этот email был удалён");
        }

        if (existingUser is null)
        {
            throw new HttpDetailException(400, "Вам сначала нужно зарегистрироваться");
        }

        var existingPartner = await _db.PartnerRequests.FirstOrDefaultAsync(x => x.UserEmail == existingUser.Email, cancellationToken);
        if (existingPartner is not null)
        {
            if (existingPartner.Status == PartnerRequestStatus.Pending)
            {
                throw new HttpDetailException(400, "Ваше сотрудничество уже рассматривается!");
            }

            if (existingPartner.Status == PartnerRequestStatus.Rejected)
            {
                throw new HttpDetailException(400, "Извините, ваша заявка отклонена!");
            }

            if (existingPartner.Status == PartnerRequestStatus.Approved)
            {
                throw new HttpDetailException(400, "Этот аккаунт уже является партнером!");
            }
        }

        _db.PartnerRequests.Add(new PartnerRequest
        {
            UserEmail = existingUser.Email,
            CompanyName = data.CompanyName,
            ContactPerson = data.FullName,
            Phone = data.Phone,
            Description = data.Description,
            Status = PartnerRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(User User, string Token)> LoginAsync(UserLogin data, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == data.Email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(data.Password, user.PasswordHash))
        {
            throw new HttpDetailException(401, "Неверный email или пароль");
        }

        if (!user.IsActive)
        {
            throw new HttpDetailException(401, "Этот аккаунт является удаленным");
        }

        return (user, _jwt.CreateAccessToken(user));
    }

    public async Task<User> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new HttpDetailException(401, "Пользователь не найден или удалён");
        }

        return user;
    }

    public async Task DeleteMeAsync(User current, UserLogin data, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == data.Email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(data.Password, user.PasswordHash))
        {
            throw new HttpDetailException(401, "Неверный email или пароль");
        }

        current.IsActive = false;
        current.DeletedAt = DateTime.UtcNow;
        current.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> RecoverAccountAsync(UserLogin data, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == data.Email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(data.Password, user.PasswordHash))
        {
            throw new HttpDetailException(401, "Неверный email или пароль");
        }

        if (user.IsActive)
        {
            return "На данный момент аккаунт не является удаленным!";
        }

        user.IsActive = true;
        user.DeletedAt = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return "Аккаунт восстановлен!";
    }
}
