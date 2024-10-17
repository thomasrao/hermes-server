using System.Text.RegularExpressions;

namespace HermesSocketServer.Validators
{
    public class VoiceNameValidator : RegexValidator
    {
        public VoiceNameValidator()
            : base("^[a-z0-9_\\-]{2,32}$", RegexOptions.IgnoreCase, 2, 32) { }
    }
}