// ============================================
// File: DevPioneers.Application/Features/Users/Commands/CreateUserCommand.cs
// Example: User Management with Admin Role
// ============================================
using DevPioneers.Application.Common.Attributes;
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DevPioneers.Application.Features.Users.Commands;

[RequireAdminRole]
[RequirePermission("CanManageUsers")]
public class CreateUserCommand : IRequest<Result<int>>
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<int>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateUserCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public Task<Result<int>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Implementation here...
        return Task.FromResult(Result<int>.Success(1));
    }
}
