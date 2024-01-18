using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using SimpleApi.Models;
using SimpleApi.Validation;

namespace SimpleApi.Services
{
    public interface IPersonService
    {
        Task<Person> CreatePerson(Person person);
        Task<bool> UpdatePerson(int personId, Person updatePerson);
        Task<bool> DeletePerson(int personId);
    }
    public class PersonService : IPersonService
    {
        private readonly IConfiguration _configuration;

        public PersonService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private DbConnection Connection => new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

        public async Task<Person> CreatePerson(Person person)
        {
            try
            {
                var personValidator = new PersonValidator(_configuration);
                var validatorResults = await personValidator.ValidateAsync(person);
                if (!validatorResults.IsValid)
                {
                    return null;
                }
                else
                {
                    using (var connection = Connection)
                    {
                        connection.Open();
                        person.PersonAddress.Id = connection.QuerySingle<int>("INSERT INTO Addresses (Country, City) VALUES (@Country, @City); SELECT CAST(SCOPE_IDENTITY() AS INT)", person.PersonAddress);
                        person.AddressId = person.PersonAddress.Id;
                        connection.Execute("INSERT INTO Persons (FirstName, LastName, Email, Age, AddressId) VALUES (@FirstName, @LastName, @Email, @Age, @AddressId)", person);
                    }
                }

                return person;
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Exception: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public async Task<bool> DeletePerson(int personId)
        {
            try
            {
                using(var connection = Connection)
                {
                    connection.Open();
                    var existingPerson = connection.QueryFirstOrDefault<Person>(
                            "SELECT * FROM Persons WHERE Id = @Id", new { Id = personId });
                    if(existingPerson == null)
                    {
                        return false;
                    }
                    var addressId = existingPerson.AddressId;
                    connection.Execute("DELETE FROM Persons WHERE Id = @Id", new { Id = personId });
                    connection.Execute("DELETE FROM Addresses WHERE Id = @AddressId", new { AddressId = addressId });
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdatePerson(int personId, Person updatePerson)
        {
            try
            {
                using (var connection = Connection)
                {
                    connection.Open();
                    var existingPerson = await connection.QuerySingleOrDefaultAsync<Person>("SELECT * FROM Persons WHERE Id = @Id", new { Id = personId });
                    if (existingPerson == null)
                    {
                        return false;
                    }
                    else
                    {
                        var personValidator = new PersonValidator(_configuration);
                        var validatorResults = personValidator.Validate(updatePerson);
                        if (!validatorResults.IsValid)
                        {
                            return false;
                        }
                        else
                        {
                            existingPerson.FirstName = updatePerson.FirstName;
                            existingPerson.LastName = updatePerson.LastName;
                            existingPerson.Age = updatePerson.Age;
                            existingPerson.Email = updatePerson.Email;
                            existingPerson.PersonAddress.Country = updatePerson.PersonAddress.Country;
                            existingPerson.PersonAddress.City = updatePerson.PersonAddress.City;

                            await connection.ExecuteAsync("UPDATE Persons SET FirstName = @FirstName, LastName = @LastName, Age = @Age, Email = @Email WHERE Id = @Id", new
                            {
                                updatePerson.FirstName,
                                updatePerson.LastName,
                                updatePerson.Age,
                                updatePerson.Email,
                                Id = personId
                            });

                            await connection.ExecuteAsync("UPDATE Addresses SET Country = @Country, City = @City WHERE Id = @Id", new
                            {
                                updatePerson.PersonAddress.Country,
                                updatePerson.PersonAddress.City,
                                Id = existingPerson.AddressId
                            });

                            return true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
