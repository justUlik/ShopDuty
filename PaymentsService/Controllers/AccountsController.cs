using Microsoft.AspNetCore.Mvc;
using PaymentsService.Services;

namespace PaymentsService.Controllers
{
    [ApiController]
    [Route("accounts")]
    public class AccountsController : ControllerBase
    {
        private readonly AccountService _accountService;

        public AccountsController(AccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromHeader(Name = "user_id")] Guid userId)
        {
            try
            {
                var account = await _accountService.CreateAccountAsync(userId);
                return Ok(new { account.UserId, account.Balance });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{userId}/topup")]
        public async Task<IActionResult> TopUp(Guid userId, [FromBody] TopUpRequest request)
        {
            try
            {
                var account = await _accountService.TopUpAccountAsync(userId, request.Amount);
                return Ok(new { account.UserId, account.Balance });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet("{userId}/balance")]
        public async Task<IActionResult> GetBalance(Guid userId)
        {
            try
            {
                var balance = await _accountService.GetBalanceAsync(userId);
                return Ok(new { userId, balance });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        public record TopUpRequest(decimal Amount);
    }
}