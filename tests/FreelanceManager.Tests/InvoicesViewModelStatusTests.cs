using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreelanceManager.App.Services;
using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

public class InvoicesViewModelStatusTests
{
    private static readonly DateTime Today = new(2026, 6, 18);

    private sealed class FakeInvoiceRepo : IInvoiceRepository
    {
        public readonly List<Invoice> Store = new();
        public Task<List<Invoice>> GetAllAsync() => Task.FromResult(Store.ToList());
        public Task<Invoice?> GetAsync(int id) => Task.FromResult(Store.FirstOrDefault(i => i.Id == id));
        public Task<Invoice> AddAsync(Invoice i) { Store.Add(i); return Task.FromResult(i); }
        public Task UpdateAsync(Invoice i) { var x = Store.FindIndex(s => s.Id == i.Id); if (x >= 0) Store[x] = i; return Task.CompletedTask; }
        public Task DeleteAsync(int id) { Store.RemoveAll(i => i.Id == id); return Task.CompletedTask; }
        public Task<int> GetMaxSequenceForYearAsync(int year) => Task.FromResult(0);
    }

    private sealed class FakeClientRepo : IClientRepository
    {
        public Task<List<Client>> GetAllAsync() => Task.FromResult(new List<Client>());
        public Task<Client?> GetAsync(int id) => Task.FromResult<Client?>(null);
        public Task<Client> AddAsync(Client c) => Task.FromResult(c);
        public Task UpdateAsync(Client c) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class FakeProjectRepo : IProjectRepository
    {
        public Task<List<Project>> GetAllAsync() => Task.FromResult(new List<Project>());
        public Task<List<Project>> GetByClientAsync(int clientId) => Task.FromResult(new List<Project>());
        public Task<Project?> GetAsync(int id) => Task.FromResult<Project?>(null);
        public Task<Project> AddAsync(Project p) => Task.FromResult(p);
        public Task UpdateAsync(Project p) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class FakeProfiles : IBusinessProfileRepository
    {
        public Task<BusinessProfile> GetAsync() => Task.FromResult(new BusinessProfile { Id = 1 });
        public Task SaveAsync(BusinessProfile profile) => Task.CompletedTask;
    }

    private sealed class FakePdf : IPdfExporter
    {
        public void ExportInvoice(Invoice invoice, BusinessProfile profile, string outputPath) { }
    }

    private sealed class FakePayments : IPaymentRepository
    {
        public readonly List<Payment> Store = new();
        public Task<List<Payment>> GetForInvoiceAsync(int invoiceId) =>
            Task.FromResult(Store.Where(p => p.InvoiceId == invoiceId).ToList());
        public Task<decimal> GetTotalPaidAsync(int invoiceId) =>
            Task.FromResult(Store.Where(p => p.InvoiceId == invoiceId).Sum(p => p.Amount));
        public Task AddAsync(Payment p) { p.Id = Store.Count + 1; Store.Add(p); return Task.CompletedTask; }
        public Task DeleteAsync(int id) { Store.RemoveAll(p => p.Id == id); return Task.CompletedTask; }
    }

    private sealed class FakeEmail : IEmailSender
    {
        public bool IsConfigured(BusinessProfile profile) => true;
        public Task TestConnectionAsync(BusinessProfile profile, string? plainPassword) => Task.CompletedTask;
        public Task SendAsync(BusinessProfile profile, string toEmail, string? toName,
                              string subject, string body, string attachmentPath) => Task.CompletedTask;
    }

    private sealed class FixedClock : IClock { public DateTime Today { get; init; } }

    private sealed class FakeDialogs : IDialogService
    {
        public Task<bool> ConfirmAsync(string t, string m, string c = "Confirm", string x = "Cancel") => Task.FromResult(true);
        public Task<bool> ShowDialogAsync(object vm) => Task.FromResult(false);
    }

    private sealed class FakeNotes : INotificationService
    {
        public void Show(string message, NotificationKind kind = NotificationKind.Info) { }
    }

    private static InvoicesViewModel CreateVm(FakeInvoiceRepo invoices, IPaymentRepository? payments = null)
        => new(invoices, new FakeClientRepo(), new FakeProjectRepo(), new InvoiceNumberGenerator(),
               new FakeProfiles(), new FakePdf(), payments ?? new FakePayments(), new FakeEmail(),
               new FixedClock { Today = Today }, new FakeDialogs(), new FakeNotes());

    [Fact]
    public async Task SetStatus_marks_paid_and_clears_overdue()
    {
        var repo = new FakeInvoiceRepo();
        repo.Store.Add(new Invoice { Id = 1, Number = "INV-1", Status = InvoiceStatus.Sent, DueDate = Today.AddDays(-5) });
        var vm = CreateVm(repo);
        vm.Selected = new InvoiceRow(repo.Store[0], InvoiceStatus.Overdue);

        await vm.SetStatusCommand.ExecuteAsync(InvoiceStatus.Paid);

        Assert.Equal(InvoiceStatus.Paid, (await repo.GetAsync(1))!.Status);
        Assert.Equal(InvoiceStatus.Paid, vm.Invoices.Single(r => r.Id == 1).Status);
    }

    [Fact]
    public async Task SetStatus_sent_on_past_due_shows_overdue_effective()
    {
        var repo = new FakeInvoiceRepo();
        repo.Store.Add(new Invoice { Id = 1, Number = "INV-1", Status = InvoiceStatus.Draft, DueDate = Today.AddDays(-5) });
        var vm = CreateVm(repo);
        vm.Selected = new InvoiceRow(repo.Store[0], InvoiceStatus.Draft);

        await vm.SetStatusCommand.ExecuteAsync(InvoiceStatus.Sent);

        Assert.Equal(InvoiceStatus.Sent, (await repo.GetAsync(1))!.Status);
        Assert.Equal(InvoiceStatus.Overdue, vm.Invoices.Single(r => r.Id == 1).Status);
    }

    [Fact]
    public async Task RecordPayment_marks_invoice_paid_when_balance_cleared()
    {
        var repo = new FakeInvoiceRepo();
        var inv = new Invoice
        {
            Id = 1, Number = "INV-1", Status = InvoiceStatus.Sent,
            LineItems = { new InvoiceLineItem { Quantity = 1, UnitPrice = 100m } }
        };
        repo.Store.Add(inv);
        var payments = new FakePayments();
        var vm = CreateVm(repo, payments);
        vm.Selected = new InvoiceRow(inv, InvoiceStatus.Sent);   // fakes complete sync → Editor is set
        vm.NewPaymentAmount = 100m;

        await vm.RecordPaymentCommand.ExecuteAsync(null);

        Assert.Single(payments.Store);
        Assert.Equal(InvoiceStatus.Paid, (await repo.GetAsync(1))!.Status);
    }

    [Fact]
    public async Task RecordPayment_leaves_status_when_partial()
    {
        var repo = new FakeInvoiceRepo();
        var inv = new Invoice
        {
            Id = 1, Number = "INV-1", Status = InvoiceStatus.Sent,
            LineItems = { new InvoiceLineItem { Quantity = 1, UnitPrice = 100m } }
        };
        repo.Store.Add(inv);
        var vm = CreateVm(repo, new FakePayments());
        vm.Selected = new InvoiceRow(inv, InvoiceStatus.Sent);
        vm.NewPaymentAmount = 40m;

        await vm.RecordPaymentCommand.ExecuteAsync(null);

        Assert.Equal(InvoiceStatus.Sent, (await repo.GetAsync(1))!.Status);
    }

    [Fact]
    public async Task SetStatus_noop_when_nothing_selected()
    {
        var repo = new FakeInvoiceRepo();
        repo.Store.Add(new Invoice { Id = 1, Number = "INV-1", Status = InvoiceStatus.Draft, DueDate = Today.AddDays(-5) });
        var vm = CreateVm(repo);

        await vm.SetStatusCommand.ExecuteAsync(InvoiceStatus.Paid);

        Assert.Equal(InvoiceStatus.Draft, repo.Store[0].Status);
    }
}
