using System;
using System.Collections.Generic;
using System.Text;


namespace SmartCare.Application.Interfaces.Security
{
    public interface ICurrentUser
    {
        bool IsAuthenticated { get; }

        string? UserId { get; }

        string? UserName { get; }

        IReadOnlyCollection<string> Roles { get; }

        bool IsInRole(string role);
    }
}