using System.ComponentModel.DataAnnotations;

namespace CSCourse.Validators
{
    public class DateTimeValidator : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var startAtProperty = validationContext.ObjectType.GetProperty("StartAt");
            var endAtProperty = validationContext.ObjectType.GetProperty("EndAt");

            if (startAtProperty == null || endAtProperty == null)
                return new ValidationResult("Required properties not found.");

            var startDate = (DateTime)startAtProperty.GetValue(validationContext.ObjectInstance);
            var endDate = (DateTime)endAtProperty.GetValue(validationContext.ObjectInstance);

            if (endDate <= startDate)
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}
