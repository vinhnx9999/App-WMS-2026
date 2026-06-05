using WMS.Domain.Common;

namespace WMS.Domain.Entities
{
    public class CodeSequence : BaseEntity
    {
        /// <summary>
        /// Type of code sequence (e.g., "SKU", "PRODUCT", etc.)
        /// </summary>
        public string CodeType { get; private set; } = default!;
        /// <summary>
        /// Prefix for the generated codes (e.g., "SKU-", "PROD-", etc.)
        /// </summary>
        public string Prefix { get; private set; } = default!;
        /// <summary>
        /// Current number in the sequence. The next generated code will increment this number.
        /// </summary>
        public int CurrentNumber { get; private set; }

        /// <summary>
        /// Total length of the numeric part of the code. For example, if PaddingLength is 6, the number 1 will be formatted as "000001".
        /// </summary>
        public int PaddingLength { get; private set; }

        /// <summary>
        /// RowVersion for concurrency control
        /// </summary>
        public byte[] RowVersion { get; private set; } = default!;

        private CodeSequence() { }
        public CodeSequence(
            string codeType,
            string prefix,
            int currentNumber = 0,
            int paddingLength = 6)
        {
            CodeType = codeType;
            Prefix = NormalizePrefix(prefix);
            CurrentNumber = currentNumber;
            PaddingLength = paddingLength;
        }

        public string Next()
        {
            if (!IsDeleted)
                throw new DomainException("Code sequence is inactive.");

            CurrentNumber++;

            var numberPart = CurrentNumber
                .ToString()
                .PadLeft(PaddingLength, '0');

            return $"{Prefix}{numberPart}";
        }


        public void ChangePrefix(string prefix)
        {
            Prefix = NormalizePrefix(prefix);
        }


        private static string NormalizePrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new DomainException("Prefix is required.");

            return prefix.Trim().ToUpperInvariant();
        }
    }
}
