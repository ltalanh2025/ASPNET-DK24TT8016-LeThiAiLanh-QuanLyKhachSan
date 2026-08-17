using System;
using QLKS.Data;
using QLKS.Infrastructure;

namespace QLKS.Services
{
    public interface IPaymentGateway
    {
        PaymentGatewayResult ProcessDeposit(decimal amount, bool simulateSuccess);
    }

    public class PaymentGatewayResult
    {
        public bool Succeeded { get; set; }
        public string TransactionCode { get; set; }
        public string Message { get; set; }
    }

    public class MockPaymentGateway : IPaymentGateway
    {
        public PaymentGatewayResult ProcessDeposit(decimal amount, bool simulateSuccess)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            return new PaymentGatewayResult
            {
                Succeeded = simulateSuccess,
                TransactionCode = "MOCK-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant(),
                Message = simulateSuccess ? "Thanh toán mô phỏng thành công." : "Giao dịch mô phỏng đã bị từ chối."
            };
        }
    }

    public class PaymentService
    {
        private readonly IPaymentGateway gateway;

        public PaymentService(IPaymentGateway gateway)
        {
            this.gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        }

        public ThanhToanCoc CreateDepositTransaction(int bookingId, decimal amount, bool simulateSuccess, DateTime now)
        {
            var gatewayResult = gateway.ProcessDeposit(amount, simulateSuccess);
            return new ThanhToanCoc
            {
                MaDatPhong = bookingId,
                MaGiaoDich = gatewayResult.TransactionCode,
                SoTien = amount,
                PhuongThuc = "MockGateway",
                TrangThai = gatewayResult.Succeeded ? DepositPaymentStatus.Succeeded : DepositPaymentStatus.Failed,
                ThoiGianTao = now,
                ThoiGianThanhToan = gatewayResult.Succeeded ? (DateTime?)now : null,
                NoiDung = gatewayResult.Message
            };
        }
    }
}
