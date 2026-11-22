using System;
using LegacyLens.Domain;

namespace LegacyLens.Application
{
    public interface ICustomerStore
    {
        int Save(Customer customer, Actor actor, string correlationId);
    }

    public static class CustomerRules
    {
        public static void Validate(Customer customer, Actor actor)
        {
            if (actor == null || !actor.CanManage) throw new UnauthorizedAccessException();
            if (customer == null ||
                string.IsNullOrWhiteSpace(customer.Name) || customer.Name.Trim().Length > 120 ||
                string.IsNullOrWhiteSpace(customer.Industry) || customer.Industry.Trim().Length > 80)
                throw new ArgumentException("請填寫客戶名稱（最多 120 字）及產業（最多 80 字）。");
        }
    }

    public sealed class CustomerService
    {
        private readonly ICustomerStore store;
        public CustomerService(ICustomerStore store) { this.store = store; }

        public int Save(Customer customer, Actor actor, string correlationId)
        {
            CustomerRules.Validate(customer, actor);
            customer.Name = customer.Name.Trim();
            customer.Industry = customer.Industry.Trim();
            return store.Save(customer, actor, correlationId);
        }
    }
}
