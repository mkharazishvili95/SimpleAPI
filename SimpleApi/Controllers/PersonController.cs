using Microsoft.AspNetCore.Mvc;
using SimpleApi.Models;
using SimpleApi.Services;
using SimpleApi.Validation;
using System;
using System.Data.SqlClient;
using System.Data;
using System.Data.Common;
using Dapper;
using System.Threading.Tasks;
using System.Transactions;

namespace SimpleApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class PersonController : ControllerBase
    {
        private readonly IPersonService _personService;
        private readonly IConfiguration _configuration;
        public PersonController(IPersonService personService, IConfiguration configuration)
        {
            _personService = personService;
            _configuration = configuration;
        }
        private DbConnection Connection => new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

        [HttpPost("CreatePerson")]
        public async Task<IActionResult> CreatePerson(Person newPerson)
        {
            try
            {
                var personValidator = new PersonValidator(_configuration);
                var validatorResults = personValidator.Validate(newPerson);

                if (!validatorResults.IsValid)
                {
                    return BadRequest(new { Message = "Validation failed!", Errors = validatorResults.Errors });
                }
                else
                {
                    await _personService.CreatePerson(newPerson);
                }
                
                return Ok(new { Message = $"Person: {newPerson.FirstName} has been successfully created!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, new { Message = "Internal Server Error", Exception = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpGet("GetAllPersons")]
        public async Task<IActionResult> GetAllPersons()
        {
            using (var connection = Connection)
            {
                connection.Open();
                var personList = await connection.QueryAsync(
                    "SELECT P.*, A.Country, A.City FROM Persons P JOIN Addresses A ON P.AddressId = A.Id");
                if (personList == null)
                {
                    return null;
                }
                else
                {
                    return Ok(personList.ToList());
                }
            }
        }

        [HttpGet("GetPersonById")]
        public async Task<IActionResult> GetPersonById(int personId)
        {
            using (var connection = Connection)
            {
                connection.Open();
                var getPersonById = await connection.QueryAsync<Person, Address, Person>(
                    "SELECT P.*, A.* FROM Persons P JOIN Addresses A ON P.AddressId = A.Id WHERE P.Id = @Id",
                    (person, address) =>
                    {
                        person.PersonAddress = address;
                        return person;
                    },
                    new { Id = personId },
                    splitOn: "Id");

                if (getPersonById == null)
                {
                    return NotFound(new { Message = $"There is no person with ID: {personId}" });
                }
                else
                {
                    return Ok(getPersonById);
                }
            }
        }

        [HttpGet("GetPersonsByCity")]
        public async Task<IActionResult> GetPersonsByCity(string city)
        {
            using (var connection = Connection)
            {
                connection.Open();
                var getPersonsByCity = await connection.QueryAsync<Person, Address, Person>(
                    "SELECT P.*, A.* FROM Persons P JOIN Addresses A ON P.AddressId = A.Id WHERE A.City LIKE @City",
                    (person, address) =>
                    {
                        person.PersonAddress = address;
                        return person;
                    },
                    new { City = $"%{city}%" },
                    splitOn: "Id");

                if (getPersonsByCity == null || !getPersonsByCity.Any())
                {
                    return NotFound(new { Message = $"There is no any person from: {city}" });
                }
                else
                {
                    return Ok(getPersonsByCity);
                }
            }
        }

        [HttpPut("UpdatePerson")]
        public async Task<IActionResult> UpdatePerson(int personId, Person updatePerson)
        {
            using(var connection = Connection)
            {
                connection.Open();
                var existingPerson = await connection.QuerySingleOrDefaultAsync<Person>("SELECT * FROM Persons WHERE Id = @Id", new { Id = personId });
                if(existingPerson == null)
                {
                    return BadRequest(new { Error = $"There is no any person by ID: {personId} to update!" });
                }
                else
                {
                    var personValidator = new PersonValidator(_configuration);
                    var validatorResults = personValidator.Validate(updatePerson);
                    if (!validatorResults.IsValid)
                    {
                        return BadRequest(validatorResults.Errors);
                    }
                    else
                    {
                        await _personService.UpdatePerson(personId, updatePerson);
                        return Ok(new { SuccessMessage = "Person has successfully updated in the database!" });
                    }
                }
            }
        }

        [HttpDelete("DeletePerson")]
        public async Task<IActionResult> DeletePerson(int personId)
        {
            using(var connection = Connection)
            {
                connection.Open();
                var existingPerson = await connection.QueryFirstOrDefaultAsync(
                    "SELECT * FROM Persons WHERE Id = @Id", new { Id = personId });
                if(existingPerson == null)
                {
                    return BadRequest(new { Error = $"There is no any person by ID: {personId} to delete!" });
                }
                else
                {
                    await _personService.DeletePerson(personId);
                    return Ok(new { SuccessMessage = "Person has been successfully deleted from the database!" });
                }
            }
        }

    }
}
