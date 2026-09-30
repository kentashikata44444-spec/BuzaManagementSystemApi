using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BuzaiManagementApi.Repositories;

namespace BuzaiManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommonController : ControllerBase
    {
        private readonly CommonRepository _commonRepository;

        public CommonController(CommonRepository commonRepository)
        {
            _commonRepository = commonRepository;
        }

        // DELETE: api/Common/login/{userCd}
        [HttpDelete("login/{userCd}")]
        public async Task<IActionResult> DeleteLogin(string userCd)
        {
            if (string.IsNullOrWhiteSpace(userCd))
            {
                return BadRequest(new { success = false, message = "ユーザーIDが指定されていません。" });
            }

            int affectedRows = await _commonRepository.DeleteLoginInfoAsync(userCd);

            return Ok(new { success = true, deletedRows = affectedRows });
        }
    }
}