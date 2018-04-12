[EnableCors("AllowAll")]
    [Route("api/[controller]")]
    //[Produces("application/json")]   
    //[Authorize]
    public class LoginController : Controller
    {        
        Scholars.Data.Business.ILoginCls _ILoginCls;
        private readonly AppSettings _appSettings;
        public LoginController(Scholars.Data.Business.ILoginCls iLoginCls,
            IOptions<AppSettings> appSettings)
        {
            _ILoginCls = iLoginCls;
            _appSettings = appSettings.Value;
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async System.Threading.Tasks.Task<TResult> Authenticate([FromBody]Scholars.Data.Business.LoginModel loginModel)
        {            
            var result = await _ILoginCls.Authenticate(loginModel);            
            string tokenString = "";
            if (result.Status == 1)
            {                
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_appSettings.SecretKey);
                Dictionary<string, dynamic> data = result.KeyData;
                User user = data.FirstOrDefault(x => x.Key == "user").Value;             
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                      {
                     new Claim(ClaimTypes.Name, user.AID.ToString())
                      }),
                    Expires = DateTime.UtcNow.AddDays(7),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                tokenString = tokenHandler.WriteToken(token);                
                result.KeyData.Add(Constants.USER_KEY, tokenString);                                
            }            
            return result;           
        }
    }