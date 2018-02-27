using System;
using System.Configuration;
using System.Net;
using System.Threading.Tasks;
using PaySimpleSdk.Accounts;
using PaySimpleSdk.Customers;
using PaySimpleSdk.Exceptions;
using PaySimpleSdk.Helpers;
using PaySimpleSdk.Models;
using PaySimpleSdk.Payments;

namespace PaySimple.DotNet.Sdk.Sample
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			while (true)
			{
				try
				{
					// Load your settings from configuration.
					var username = ConfigurationManager.AppSettings["Username"];
					if (string.IsNullOrWhiteSpace(username))
					{
						Console.WriteLine("Username is missing from App.Config");
						return;
					}

					// We recommend storing your ApiKey as an encrypted value in production.
					var apiKey = ConfigurationManager.AppSettings["ApiKey"];
					if (string.IsNullOrWhiteSpace(apiKey))
					{
						Console.WriteLine("ApiKey is missing from App.Config");
						return;
					}

					var baseUrl = ConfigurationManager.AppSettings["ApiUrl"];
					if (string.IsNullOrWhiteSpace(baseUrl))
					{
						Console.WriteLine("ApiUrl is missing from App.Config");
						return;
					}

					var settings = new PaySimpleSettings(apiKey, username, baseUrl);

					var customerService = new CustomerService(settings);
					CreditCard defaultAccount;
					Customer customer;
					
					// In your system, you would store this PaySimple customer Id in your own database.
					// To make this example work, you need to enter an exising customer id in your PaySimple instance.
					Console.WriteLine("This is an example of making a payment with an existing customer.");
					Console.WriteLine("In your production code, the PaySimple customer Id would most likely be stored in your database");
					Console.Write("Enter PaySimple Customer Id: ");
					var customerIdString = Console.ReadLine();
					var customerId = int.Parse(customerIdString);

					// First, grab the customer to make sure it exists.
					// You could also store the customer's account in your database to avoid this lookup.
					try
					{
						customer = await customerService.GetCustomerAsync(customerId);
						Console.WriteLine($"Customer {customer.FirstName} {customer.LastName} selected.");
						Console.WriteLine("Now selecting the default credit card account for the customer.");
						Console.WriteLine("You could also store the credit card account id in your database or add a new one to the customer.");
					}
					catch (PaySimpleEndpointException e)
					{
						HandleException(e, $"Customer with Id {customerId} does not exist");
						return;
					}

					// Second, grab the customers default credit card or ach account. 
					// You could also store the account number in your database when you created it to avoid this lookup.
					try
					{
						defaultAccount = await customerService.GetDefaultCreditCardAccountAsync(customer.Id);
						Console.WriteLine($"Default Credit Card Account {defaultAccount.Issuer} ends in {defaultAccount.CreditCardNumber}");
					}
					catch (PaySimpleEndpointException e)
					{
						HandleException(e, $"Default credit card for customer with Id {customerId} does not exist");
						return;
					}

					Console.WriteLine("Note: To simulate a failure enter a payment amount 9999.01 - 9999.29. For success, enter any other amount.");
					Console.Write("Enter Payment Amount: ");
					var amt = Console.ReadLine();
					var amount = decimal.Parse(amt);

					var paymentService = new PaymentService(settings);
					var makePaymentRequest = new Payment
					{
						AccountId = defaultAccount.Id,
						Amount = amount
					};

					// Make the payment
					Console.WriteLine($"Making payment for ${amount:0.00}...");
					var paymentResponse = await paymentService.CreatePaymentAsync(makePaymentRequest);
					Console.WriteLine($"Payment {paymentResponse.Id} is in status {paymentResponse.Status}");

					// Payment has failed.  To simulate a failure, enter a payment amount of $9999.01
					if (paymentResponse.Status.HasValue && paymentResponse.Status.Value == Status.Failed)
						Console.WriteLine($"Failure code: '{paymentResponse.FailureData.Code}'; Description: '{paymentResponse.FailureData.Description}'; Corrective Action: '{paymentResponse.FailureData.MerchantActionText}'");
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}

				Console.WriteLine("Would you like to make another payment? Y / N");
				var yn = Console.ReadKey();

				if (yn.KeyChar != 'Y' && yn.KeyChar != 'y')
					return;

				Console.WriteLine();
			}

			void HandleException(PaySimpleEndpointException e, string notFoundMessage)
			{
				if (e.StatusCode == HttpStatusCode.NotFound)
				{
					Console.WriteLine(notFoundMessage);
				}
				else if (e.StatusCode == HttpStatusCode.BadRequest)
				{
					foreach (var error in e.EndpointErrors.ResultData.Errors.ErrorMessages)
					{
						if (!string.IsNullOrWhiteSpace(error.Field))
							Console.WriteLine($"Bad Request. Field:{error.Field} Message: {error.Message}");

						Console.WriteLine($"Bad Request: {error.Message}");
					}
				}
				else
				{
					Console.WriteLine($"{e.StatusCode}: {e.Message}");
				}

				Console.WriteLine("Press a key to exit");
				Console.ReadKey();
			}
		}
	}
}
