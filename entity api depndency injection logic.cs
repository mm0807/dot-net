 [Table("LeaveYear", Schema = "hrm")]
    public class LeaveYear
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public short AID { get; set; }
        [Required]
        public string Code { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool IsPrevLY { get; set; }
        public short? PreviousLY_AID { get; set; }        
        /// <summary>
        /// 1 = Autogenerate Previous FY ; 0 = Manualy Created By User
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [DefaultValue(1)]
        public bool IsVisible { get; set; }
        /// <summary>
        /// only last 2 year transaction is active
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]              
        public bool IsActive { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int CreatedBy { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int ModifyBy { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime Created { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime InvokeTime { get; set; }
    }
	 public class LeaveYearEditCls : AuditTrailLogCls, IDisposable, ILeaveYearEditCls
    {
        private readonly AppDbContext _AppDbContext;
        public LeaveYearEditCls(AppDbContext appDbContext)
        {
            _AppDbContext = appDbContext;
        }
        public void Dispose()
        {
            _AppDbContext.Dispose();
        }
        public async Task<IEnumerable<leaveYearView>> View()
        {
            try
            {
                var query = (from b in _AppDbContext.LeaveYear
                             where b.IsVisible == true
                             orderby b.Code
                             select new leaveYearView
                             {
                                 AID = b.AID,
                                 Code = b.Code,
                                 FromDate = b.FromDate,
                                 ToDate = b.ToDate,
                                 IsActive = b.IsActive
                             });
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                await ErrorLogAsync(ex, true);
                throw ex;
            }
        }
		public async Task<TResult> Save(LeaveYear pLeaveYear, int pLoginAID)
        {
            TResult result = new TResult();
            try
            {
                DbParameter MessageReturn = null; DbParameter Return = null;
                _AppDbContext.LoadStoredProc("[hrm].[LeaveYearIU]", false)
                         .WithSqlParam("pAID", pLeaveYear.AID)
                         .WithSqlParam("pCode", pLeaveYear.Code)
                         .WithSqlParam("pFromDate", pLeaveYear.FromDate)
                         .WithSqlParam("pToDate", pLeaveYear.ToDate)
                         .WithSqlParam("pIsActive", pLeaveYear.IsActive)
                         .WithSqlParam("pLoginAID", pLoginAID)
                         .WithSqlParam("MessageReturn", (dbParam) =>
                         {
                             dbParam.Direction = ParameterDirection.Output;
                             dbParam.DbType = DbType.String;
                             dbParam.Size = 500;
                             MessageReturn = dbParam;
                         })
                         .WithSqlParam("Return", (dbParam) =>
                         {
                             dbParam.Direction = ParameterDirection.ReturnValue;
                             dbParam.DbType = DbType.Int16;
                             Return = dbParam;
                         })
                         .ExecuteStoredProc((handler) => { });
                result.Message = Convert.ToString(MessageReturn.Value);
                result.Status = Convert.ToInt16(Return.Value);
                var data = await View();
                result.Data = new List<object>(data);
                return result;
            }
            catch (Exception ex)
            {
                await ErrorLogAsync(ex, true);
                throw ex;
            }
            finally
            {
                this.Dispose();
            }
        }
	}
	 public class AppDbContext : DbContext, IDisposable
    {
        public virtual DbSet<Setting.Organization.FinancialYear> FinancialYear { get; set; }
       
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
       
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);            
        }
    }