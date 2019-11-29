public class RgArgumentsInfo : ArgumentsInfo
{
    #region Methods

    public RgArgumentsInfo(string pattern, string args, bool singleQmarks = false) : this(
        pattern,
        PathUtil.DataPath,
        args,
        singleQmarks
    )
    {
    }

    private string pattern;
    private string workDir;
    private string exArgs;

    public RgArgumentsInfo(
        string pattern,
        string workDir,
        string exArgs,
        bool singleQmarks = false
    ) : base(
        ReferenceToolSetting.ripgrepPath
    )
    {
        this.pattern = pattern;
        this.workDir = workDir;
        this.exArgs = exArgs;
        if (singleQmarks)
        {
            args = $"\'{pattern}\' \'{workDir}\' {exArgs}";
        }
        else
        {
            args = $"\"{pattern}\" \'{workDir}\' {exArgs}";
        }
    }

    public void SetWorkDir(string workDir) => args = $"\'{pattern}\' \'{workDir}\' {exArgs}";

    #endregion
}