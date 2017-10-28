// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//
//Copyright (C) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using SampleSupport;
using Task.Data;

// Version Mad01

namespace SampleQueries
{
    [Title("LINQ Module")]
    [Prefix("Linq")]
    public class LinqSamples : SampleHarness
    {

        private DataSource dataSource = new DataSource();

        [Category("Task")]
        [Title("Task 001")]
        [Description("Displays all customers with sum of orders total greater than X")]

        public void Linq001()
        {
            decimal x = 100;
            var customersList = dataSource.Customers.Select(c => new
            {
                CustomerId = c.CustomerID,
                TotalSum = c.Orders.Sum(o => o.Total)
            }).Where(c => c.TotalSum > x);

            ObjectDumper.Write($"Greater than {x}");
            foreach (var customer in customersList)
            {
                ObjectDumper.Write(
                    string.Format($"CustomerId = {customer.CustomerId} TotalSum = {customer.TotalSum}\n"));
            }
            x = 1500;
            ObjectDumper.Write($"Greater than {x}");
            foreach (var customer in customersList)
            {
                ObjectDumper.Write(
                    string.Format($"CustomerId = {customer.CustomerId} TotalSum = {customer.TotalSum}\n"));
            }
        }

        [Category("Task")]
        [Title("Task 002")]
        [Description("For each customer displays a list of suppliers from the same city and country")]

        public void Linq002()
        {
            var customersWithSuppliers = dataSource.Customers.Select(c => new
            {
                Customer = c,
                Suppliers = dataSource.Suppliers.Where(s => s.City == c.City && s.Country == c.Country)
            });

            ObjectDumper.Write("Without grouping\n");
            foreach (var cs in customersWithSuppliers)
            {
                ObjectDumper.Write($"CustomerId: {cs.Customer.CustomerID} " +
                    $"List of suppliers: {string.Join(", ", cs.Suppliers.Select(s => s.SupplierName))}");
            }

            var result = dataSource.Customers.GroupJoin(dataSource.Suppliers,
                c => new { c.City, c.Country },
                s => new { s.City, s.Country },
                (c, sups) => new { Customer = c, Suppliers = sups });

            ObjectDumper.Write("With  grouping:\n");
            foreach (var cs in result)
            {
                ObjectDumper.Write($"CustomerId: {cs.Customer.CustomerID} " +
                    $"List of suppliers: {string.Join(", ", cs.Suppliers.Select(s => s.SupplierName))}");
            }
        }

        [Category("Task")]
        [Title("Task 003")]
        [Description("Displays all customers who has order with total greater than X")]

        public void Linq003()
        {
            decimal x = 5000;
            var customers = dataSource.Customers.Where(c => c.Orders.Any(s => s.Total > x));

            foreach (var c in customers)
            {
                ObjectDumper.Write(c);
            }
        }

        [Category("Task")]
        [Title("Task 004")]
        [Description("Displays all customers with their first orders month and year")]

        public void Linq004()
        {
            var customers = dataSource.Customers.Where(c => c.Orders.Any())
                .Select(c => new
                {
                    CustomerId = c.CustomerID,
                    StartDate = c.Orders.OrderBy(o => o.OrderDate).Select(o => o.OrderDate).First()
                });

            foreach (var c in customers)
            {
                ObjectDumper.Write($"CustomerId = {c.CustomerId} " +
                    $"Month = {c.StartDate.Month} Year = {c.StartDate.Year}");
            }
        }

        [Category("Task")]
        [Title("Task 005")]
        [Description("Displays all customers with their first orders month and year ordered by" +
            "year, month, sum of orders total, clientName")]

        public void Linq005()
        {
            var customers = dataSource.Customers.Where(c => c.Orders.Any())
                .Select(c => new
                {
                    CustomerId = c.CustomerID,
                    StartDate = c.Orders.OrderBy(o => o.OrderDate).Select(o => o.OrderDate).First(),
                    TotalSum = c.Orders.Sum(o => o.Total)
                }).OrderByDescending(c => c.StartDate.Year)
                .ThenByDescending(c => c.StartDate.Month)
                .ThenByDescending(c => c.TotalSum)
                .ThenByDescending(c => c.CustomerId);

            foreach (var c in customers)
            {
                ObjectDumper.Write($"CustomerId = {c.CustomerId} TotalSum: {c.TotalSum} " +
                    $"Month = {c.StartDate.Month} Year = {c.StartDate.Year}");
            }
        }

        [Category("Task")]
        [Title("Task 006")]
        [Description("Displays all customers with not number postal code or without region " +
            "or whithout operator's code")]
        public void Linq006()
        {
            var customers = dataSource.Customers.Where(
                c => c.PostalCode != null && c.PostalCode.Any(sym => sym < '0' || sym > '9')
                    || string.IsNullOrWhiteSpace(c.Region)
                    || c.Phone.FirstOrDefault() != '(');

            foreach (var c in customers)
            {
                ObjectDumper.Write(c);
            }
        }

        [Category("Task")]
        [Title("Task 007")]
        [Description("Groups products by categories then by units in stock > 0  then order by unitPrice")]
        public void Linq007()
        {
            var groups = dataSource.Products
                .GroupBy(p => p.Category)
                .Select(gr => new
                {
                    Category = gr.Key,
                    ProductsByStock = gr.GroupBy(pr => pr.UnitsInStock > 0)
                        .Select(group => new
                        {
                            HasInStock = group.Key,
                            Products = group.OrderBy(prod => prod.UnitPrice)
                        })
                });

            foreach (var productsByCategory in groups)
            {
                ObjectDumper.Write($"Category: {productsByCategory.Category}\n");
                foreach (var productsByStock in productsByCategory.ProductsByStock)
                {
                    ObjectDumper.Write($"\tHas in stock: {productsByStock.HasInStock}");
                    foreach (var product in productsByStock.Products)
                    {
                        ObjectDumper.Write($"\t\tProduct: {product.ProductName} Price: {product.UnitPrice}");
                    }
                }
            }
        }

        [Category("Task")]
        [Title("Task 008")]
        [Description("Groups products by price: Cheap, Average price, Expensive")]
        public void Linq008()
        {
            decimal lowAverageBoundary = 20;
            decimal averageExpensiveBoundary = 50;

            var productGroups = dataSource.Products
                .GroupBy(p => p.UnitPrice < lowAverageBoundary ? "Cheap"
                    : p.UnitPrice < averageExpensiveBoundary ? "Average price" : "Expensive");

            foreach (var group in productGroups)
            {
                ObjectDumper.Write($"{group.Key}:");
                foreach (var product in group)
                {
                    ObjectDumper.Write($"\tProduct: {product.ProductName} Price: {product.UnitPrice}\n");
                }
            }
        }

        [Category("Task")]
        [Title("Task 009")]
        [Description("Counts average order sum for and average client's intensity for " +
            "every city")]
        public void Linq009()
        {
            var results = dataSource.Customers
                .GroupBy(c => c.City)
                .Select(cityCustomers => new
                {
                    City = cityCustomers.Key,
                    Intensity = cityCustomers.Average(cityCustomer => cityCustomer.Orders.Count()),
                    AverageIncome = cityCustomers.Average(cityCustomer => cityCustomer.Orders.Sum(o => o.Total))
                });

            foreach (var group in results)
            {
                ObjectDumper.Write($"City: {group.City}");
                ObjectDumper.Write($"\tIntensity: {group.Intensity}");
                ObjectDumper.Write($"\tAverage Income: {group.AverageIncome}");
            }
        }

        [Category("Task")]
        [Title("Task 010")]
        [Description("Displays clients activity statistic by month (without year), by year and" +
            " by year and month")]
        public void Linq010()
        {
            var statistic = dataSource.Customers
                .Select(c => new
                {
                    c.CustomerID,
                    MonthsStatistic = c.Orders.GroupBy(order => order.OrderDate.Month)
                                        .Select(gr => new { Month = gr.Key, OrdersCount = gr.Count() }),
                    YearsStatistic = c.Orders.GroupBy(order => order.OrderDate.Year)
                                        .Select(gr => new { Year = gr.Key, OrdersCount = gr.Count() }),
                    YearMonthStatistic = c.Orders
                                        .GroupBy(order => new { order.OrderDate.Year, order.OrderDate.Month })
                                        .Select(gr => new {  gr.Key.Year, gr.Key.Month, OrdersCount = gr.Count() })
                });

            foreach (var record in statistic)
            {
                ObjectDumper.Write($"CustomerId: {record.CustomerID}");
                ObjectDumper.Write("\tMonths statistic:\n");
                foreach (var ms in record.MonthsStatistic)
                {
                    ObjectDumper.Write($"\t\tMonth: {ms.Month} Orders count: {ms.OrdersCount}");
                }
                ObjectDumper.Write("\tYears statistic:\n");
                foreach (var ys in record.YearsStatistic)
                {
                    ObjectDumper.Write($"\t\tYear: {ys.Year} Orders count: {ys.OrdersCount}");
                }
                ObjectDumper.Write("\tYear and month statistic:\n");
                foreach (var ym in record.YearMonthStatistic)
                {
                    ObjectDumper.Write($"\t\tYear: {ym.Year} Month: {ym.Month} Orders count: {ym.OrdersCount}");
                }
            }
        }
    }
}
