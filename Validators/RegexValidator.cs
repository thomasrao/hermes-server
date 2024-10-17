using System.Text.RegularExpressions;

namespace HermesSocketServer.Validators
{
    public class RegexValidator : IValidator
    {
        private readonly Regex _regex;
        private readonly int _minimum;
        private readonly int _maximum;

        public RegexValidator(string regex, RegexOptions options, int minimum, int maximum) {
            _regex = new Regex(regex, options | RegexOptions.Compiled);
            _minimum = minimum;
            _maximum = maximum;
        }

        public bool Check(string? input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (input.Length < _minimum)
                throw new ArgumentException("Too short. Must be of length 8 or greater.", nameof(input));
            if (input.Length > _maximum)
                throw new ArgumentException("Too long. Must be of length 24 or less.", nameof(input));
            
            return _regex.IsMatch(input);
        }
    }
}