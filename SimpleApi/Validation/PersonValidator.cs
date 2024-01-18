using FluentValidation;
using SimpleApi.Models;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace SimpleApi.Validation
{
    public class PersonValidator : AbstractValidator<Person>
    {
        private readonly IConfiguration _configuration;

        public PersonValidator(IConfiguration configuration)
        {
            _configuration = configuration;

            RuleFor(p => p.FirstName).NotEmpty().WithMessage("Enter your FirstName!");
            RuleFor(p => p.LastName).NotEmpty().WithMessage("Enter your LastName!");
            RuleFor(p => p.Age).NotEmpty().WithMessage("Enter your Age!")
                .GreaterThanOrEqualTo(18).WithMessage("Your age must be 18 or more!");
            RuleFor(p => p.Email).NotEmpty().WithMessage("Enter your Email Address!")
                .EmailAddress().WithMessage("Enter your valid Email Address!")
                .Must(BeUniqueEmail).WithMessage("Email address already exists. Try another!");
            RuleFor(address => address.PersonAddress).SetValidator(new AddressValidator());
        }

        private IDbConnection Connection
        {
            get
            {
                return new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            }
        }

        private bool BeUniqueEmail(string email)
        {
            using (var connection = Connection)
            {
                connection.Open();
                var result = connection.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM Persons WHERE Email = @Email", new { Email = email });
                return result == 0;
            }
        }
    }
}
