namespace FreelanceManager.App.Services;

// Sent over WeakReferenceMessenger; MainWindowViewModel listens and navigates.
public record OpenProjectMessage(int Id);
public record OpenInvoiceMessage(int Id);
public record OpenPageMessage(string Page);   // "Dashboard" | "Clients" | "Projects" | "Invoices" | "Settings"
