using System.Text.RegularExpressions;

namespace HermesSocketServer.Validators
{
    public class VoiceIdValidator : RegexValidator
    {
        public VoiceIdValidator()
            : base("^[a-z0-9]{25}$", RegexOptions.IgnoreCase, 25, 25) { }
    }
}