using System;
using System.IO;
using System.Reflection;
using YYB.Licensing;

namespace S3D.EquipmentPartArray.Services
{
    public static class LicenseGuard
    {
        private const string PublicKeyXml = "<RSAKeyValue><Modulus>zdedJhHvQBEgkQOLl2xw1DM16AIySXbMBFCymWkzpq1XFAV5Yy2e9lhSeLT/vx7HqvCs+JPZ9CKjYOaN3X/9yfLMx92GzJHXKakn2atK4Aoyu6OoYcEFrZ/kwrvHeUZ2oD32ZqFc4xwO+KIdhCu8zLoSlKbUM92d6L/m/tBk42yx/RFo6Y2w24Ccnn4q9wpe5RN0oADxTwJbZTfdJGMMDAOj05DgiC5ffMTMlUzFovzEhzEc2X3h4ayPJQrAyLqx94rRwO+jBMLLhLHVB8XEZGjmcsPGTinwoH+IshEm6SzMPn+EdmeHsfkhcDYTbskTzzmA6GvNHMkKoEMQ1P7FLQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        private const string ProductName = "S3D.EquipmentPartArray";
        private const string LicenseFileName = "S3D.EquipmentPartArray.lic";

        public static LicenseValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(PublicKeyXml))
            {
                return LicenseValidationResult.Invalid(
                    LicenseValidationStatus.InvalidSignature,
                    "License public key is not configured. Generate a key pair and embed the public key before release.");
            }

            string licensePath = FindLicensePath();
            if (licensePath == null)
            {
                return LicenseValidationResult.Invalid(
                    LicenseValidationStatus.MissingLicense,
                    "License file was not found. Place S3D.EquipmentPartArray.lic beside the DLL or in a License subfolder.");
            }

            string licenseText = File.ReadAllText(licensePath);
            return LicenseValidator.Validate(
                licenseText,
                ProductName,
                PublicKeyXml,
                LicenseMachineCode.CurrentMachineHash(),
                DateTime.UtcNow);
        }

        public static string CurrentMachineCode()
        {
            return LicenseMachineCode.CurrentMachineHash();
        }

        private static string FindLicensePath()
        {
            string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(assemblyDirectory))
            {
                return null;
            }

            string directPath = Path.Combine(assemblyDirectory, LicenseFileName);
            if (File.Exists(directPath))
            {
                return directPath;
            }

            string subfolderPath = Path.Combine(assemblyDirectory, "License", LicenseFileName);
            if (File.Exists(subfolderPath))
            {
                return subfolderPath;
            }

            return null;
        }
    }
}
