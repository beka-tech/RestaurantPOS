using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

public class User : Entity
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public string FullName => $"{FirstName} {LastName}";

    public User(string firstName, string lastName, string email, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentNullException("First Name is required.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last Name is requird. ");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentNullException("Email is required. ");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Role = role;

        IsActive = true;
    }

    public void ChangeName(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First Name is requird");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("last name is required");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();

        MarkUpdated();
    }

    public void ChangeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required. ");

        Email = email.Trim().ToLowerInvariant();

        MarkUpdated();
    }

    public void ChageRole(UserRole role)
    {
        Role = role;

        MarkUpdated();
    }

    public void Activate()
    {
        if (IsActive)
            throw new InvalidOperationException("User is already active. ");

        IsActive = true;

        MarkUpdated();
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new InvalidOperationException("User is already inactive. ");

        IsActive = false;

        MarkUpdated();
    }
}
