namespace Restaurant.Application.Common;

public static class EmployeePinStandardizer
{
    /// <summary>
    /// Chuẩn hóa mã nhân viên thành mã PIN đăng nhập POS/KDS có đúng 6 chữ số.
    /// Quy tắc: PIN được tạo bằng cách lấy mã nhân viên và thêm số '0' vào đầu cho đủ 6 ký tự.
    /// Ví dụ: "1" -> "000001", "12" -> "000012", "123456" -> "123456".
    /// </summary>
    public static string StandardizePinFromEmployeeCode(string? employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            throw new ArgumentException("Mã nhân viên không được để trống.");
        }

        var trimmed = employeeCode.Trim();

        // Loại bỏ tiền tố 'NV' hoặc 'nv' nếu người dùng nhập kèm tiền tố
        if (trimmed.StartsWith("NV", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(2).Trim();
        }

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ArgumentException("Mã nhân viên không hợp lệ.");
        }

        if (!trimmed.All(char.IsDigit))
        {
            throw new ArgumentException("Mã nhân viên chỉ được chứa các ký tự số (ví dụ: 1, 12, 123456).");
        }

        if (trimmed.Length > 6)
        {
            throw new ArgumentException("Mã nhân viên không được vượt quá 6 chữ số.");
        }

        return trimmed.PadLeft(6, '0');
    }

    /// <summary>
    /// Kiểm tra định dạng mã PIN 6 chữ số hợp lệ.
    /// </summary>
    public static bool IsValidPin(string? pin)
    {
        if (string.IsNullOrWhiteSpace(pin)) return false;
        var trimmed = pin.Trim();
        return trimmed.Length == 6 && trimmed.All(char.IsDigit);
    }
}
