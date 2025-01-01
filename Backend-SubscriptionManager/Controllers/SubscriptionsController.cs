using Backend_SubscriptionManager.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using YourProjectNamespace.Data;
using ClosedXML.Excel;

namespace Backend_SubscriptionManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly YourDbContext _context;

        public SubscriptionsController(YourDbContext context)
        {
            _context = context;
        }

        // 1. Get All Subscriptions
        [HttpGet]
        public async Task<IActionResult> GetAllSubscriptions()
        {
            var subscriptions = await _context.Subscriptions.ToListAsync();

            // Calculate statistics for the dashboard
            var totalMonthlyCost = subscriptions.Sum(s => s.Cost / 12);
            var totalAnnualCost = subscriptions.Sum(s => s.Cost);
            var activeSubscriptions = subscriptions.Count;

            return Ok(new
            {
                Subscriptions = subscriptions,
                Statistics = new
                {
                    TotalMonthlyCost = totalMonthlyCost,
                    TotalAnnualCost = totalAnnualCost,
                    ActiveSubscriptions = activeSubscriptions
                }
            });
        }

        // 2. Add Subscription
        [HttpPost]
        public async Task<IActionResult> AddSubscription([FromForm]Subscription subscription, [FromForm] IFormFile invoiceFile)
        {
            if (subscription == null || string.IsNullOrWhiteSpace(subscription.Name) ||
                string.IsNullOrWhiteSpace(subscription.Category) || subscription.Cost <= 0)
            {
                return BadRequest(new { Message = "Invalid subscription data." });
            }

            if (invoiceFile != null)
            {
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                var filePath = Path.Combine(uploadDir, invoiceFile.FileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await invoiceFile.CopyToAsync(stream);
                subscription.InvoiceFilePath = filePath;
            }

            subscription.CreatedAt = DateTime.Now;

            try
            {
                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                // Schedule email notification
                //await SendRenewalReminderEmail(subscription);

                return Ok(new { Message = "Subscription added successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Error adding subscription", Details = ex.Message });
            }
        }

        // 3. Delete Subscription
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubscription(int id)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription == null) return NotFound(new { Message = "Subscription not found." });

            _context.Subscriptions.Remove(subscription);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Subscription deleted!" });
        }

        // Helper Method: Send Email Notification
        private async Task SendRenewalReminderEmail(Subscription subscription)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("your-email@example.com", "your-email-password"),
                EnableSsl = true,
            };

            // Calculate days before renewal
            var daysBeforeRenewal = (subscription.RenewalDate - DateTime.Now).Days;

            // Only send reminders within a week of renewal
            if (daysBeforeRenewal > 7) return;

            var mailMessage = new MailMessage
            {
                From = new MailAddress("your-email@example.com"), // Sender's email
                Subject = "Subscription Renewal Reminder",
                Body = $"Hi, \n\nThis is a reminder that your subscription \"{subscription.Name}\" will renew on {subscription.RenewalDate:yyyy-MM-dd}. Please take any necessary actions.",
                IsBodyHtml = false,
            };

            // Send email to the provided recipient (in this case, 'whatweed123@gmail.com')
            mailMessage.To.Add("whatweed123@gmail.com");

            // Send the email
            await smtpClient.SendMailAsync(mailMessage);
        }

        [HttpGet("ExportToExcel")]
        public async Task<IActionResult> ExportToExcel()
        {
            var subscriptions = await _context.Subscriptions.ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Subscriptions");
                var currentRow = 1;

                // Add headers
                worksheet.Cell(currentRow, 1).Value = "ID";
                worksheet.Cell(currentRow, 2).Value = "Name";
                worksheet.Cell(currentRow, 3).Value = "Category";
                worksheet.Cell(currentRow, 4).Value = "Cost";
                worksheet.Cell(currentRow, 5).Value = "Renewal Date";
                worksheet.Cell(currentRow, 6).Value = "Created At";

                // Add subscription data
                foreach (var subscription in subscriptions)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = subscription.Id;
                    worksheet.Cell(currentRow, 2).Value = subscription.Name;
                    worksheet.Cell(currentRow, 3).Value = subscription.Category;
                    worksheet.Cell(currentRow, 4).Value = subscription.Cost;
                    worksheet.Cell(currentRow, 5).Value = subscription.RenewalDate;
                    worksheet.Cell(currentRow, 6).Value = subscription.CreatedAt;
                }

                // Prepare Excel file for download
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Subscriptions.xlsx");
                }
            }
        }
    }
}
