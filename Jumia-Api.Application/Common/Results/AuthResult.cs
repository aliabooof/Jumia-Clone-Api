namespace Jumia_Api.Api.Contracts.Results
{
    public class AuthResult
    {
        public bool Successed { get; set; }
        public string Message { get; set; }
        public string? Token { get; set; }


        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }

        public string? UserRole { get; set; }

        public int? UserTypeId { get; set; }

        public string? User { get; set; }

    }
}
