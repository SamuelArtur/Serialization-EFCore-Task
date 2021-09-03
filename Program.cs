using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Storage;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Text.Json;

using static System.IO.Path;
using static System.Console;
using static System.Environment;

namespace SerializationEFCore
{
    class Program
    {
        static void QueryingCategories()
        {
            using (var db = new Northwind())
            {
                var loggerFactory = db.GetService<ILoggerFactory>();
                loggerFactory.AddProvider(new ConsoleLoggerProvider());

                WriteLine("Categorias e quantos produtos possuem:");

                IQueryable<Category> cats = db.Categories;

                db.ChangeTracker.LazyLoadingEnabled = false;

                Write("Habilitar Eager Loading? (Y/N): ");
                bool eagerloading = (ReadKey().Key == ConsoleKey.Y);
                bool explicitloading = false;
                WriteLine();

                if (eagerloading)
                {
                    cats = db.Categories.Include(c => c.Products);
                }
                else
                {
                    cats = db.Categories;
                    Write("Habilitar Explicit Loading? (Y/N): ");
                    explicitloading = (ReadKey().Key == ConsoleKey.Y);
                    WriteLine();
                }

                foreach (Category c in cats)
                {
                    if (explicitloading)
                    {
                        Write($"Carregar produtos explicitamente em {c.CategoryName}? (Y/N): ");
                        ConsoleKeyInfo key = ReadKey();
                        WriteLine();

                        if (key.Key == ConsoleKey.Y)
                        {
                            var products = db.Entry(c).Collection(c2 => c2.Products);
                            if (!products.IsLoaded) products.Load();
                        }
                    }
                    WriteLine($"{c.CategoryName} possui {c.Products.Count} produtos.");
                }
            }
        }
        static void FilteredIncludes()
        {
            using (var db = new Northwind())
            {
                Write("Digite uma quantidade mínima para unidades em estoque: ");
                string unitsInStock = ReadLine();
                int stock = int.Parse(unitsInStock);
                IQueryable<Category> cats = db.Categories.Include(c => c.Products.Where(p => p.Stock >= stock));
                WriteLine($"ToQueryString: {cats.ToQueryString()}");

                foreach (Category c in cats)
                {
                    WriteLine($"{c.CategoryName} possui {c.Products.Count} produtos com um mínimo de {stock} unidades em estoque.");

                    foreach (Product p in c.Products)
                    {
                        WriteLine($" {p.ProductName} possui {p.Stock} unidades em estoque.");
                    }
                }
            }
        }

        static void QueryingProducts()
        {
            using (var db = new Northwind())
            {
                var loggerFactory = db.GetService<ILoggerFactory>();
                loggerFactory.AddProvider(new ConsoleLoggerProvider());

                WriteLine("Products que custam mais em preço, maiores ao topo.");
                string input;
                decimal price;
                do
                {
                    Write("Digite um preço de produto: ");
                    input = ReadLine();
                } while (!decimal.TryParse(input, out price));

                IQueryable<Product> prods = db.Products
                .TagWith("Produtos ordenados e filtrados por preço.")
                .Where(product => product.Cost > price)
                .OrderByDescending(product => product.Cost);

                foreach (Product item in prods)
                {
                    WriteLine(
                    "{0}: {1} custa {2:$#,##0.00} e há {3} em estoque.",
                    item.ProductID, item.ProductName, item.Cost, item.Stock);
                }
            }
        }
        static void QueryingWithLike()
        {
            using (var db = new Northwind())
            {
                var loggerFactory = db.GetService<ILoggerFactory>();
                loggerFactory.AddProvider(new ConsoleLoggerProvider());
                Write("Digite a parte do nome de um produto: ");
                string input = ReadLine();

                IQueryable<Product> prods = db.Products
                .Where(p => EF.Functions.Like(p.ProductName, $"%{input}%"));
                foreach (Product item in prods)
                {
                    WriteLine("{0} possui {1} unidades em estoque. Descontinuado? {2}",
                    item.ProductName, item.Stock, item.Discontinued);
                }
            }
        }
        static bool AddProduct(int categoryID, string productName, decimal? price)
        {
            using (var db = new Northwind())
            {
                var newProduct = new Product
                {
                    CategoryID = categoryID,
                    ProductName = productName,
                    Cost = price
                };

                db.Products.Add(newProduct);
                int affected = db.SaveChanges();
                return (affected == 1);
            }
        }
        [Obsolete]
        static void SerializeInCsv()
        {
            string csv = "categories_products.csv";

            using (var db = new Northwind())
            {
                IQueryable<Category> categories = db.Categories.Include(c => c.Products);

                using (FileStream stream = File.Create(csv))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.WriteLine("ID da Categoria, Nome da Categoria, Descrição, ID do Produto, Nome do Produto, Custo, Estoque, Descontinuado");

                        foreach (Category c in categories)
                        {
                            foreach (Product p in c.Products)
                            {
                                writer.Write($"{c.CategoryID} | {c.CategoryName} | {c.Description} | ");
                                writer.Write($"{p.ProductID} | {p.ProductName} | {p.Cost.Value.ToString()} | {p.Stock.ToString()} | {p.Discontinued.ToString()}\n");
                            }
                        }
                    }
                }
                Console.WriteLine($"{csv} contém {new FileInfo(csv).Length:N0} bytes");
            }
        }

        private delegate void WriteDataDelegate(string name, string value);
        static void SerializeInXml()
        {
            using (var db = new Northwind())
            {
                IQueryable<Category> categories = db.Categories.Include(c => c.Products);

                string xmlFile = "categories_products.xml";

                using (FileStream xmlStream = File.Create(Combine(CurrentDirectory, xmlFile)))
                {
                    using (XmlWriter xml = XmlWriter.Create(xmlStream, new XmlWriterSettings { Indent = true }))
                    {
                        WriteDataDelegate writeMethod;
                        writeMethod = xml.WriteElementString;

                        xml.WriteStartDocument();
                        xml.WriteStartElement("Categorias");

                        foreach (Category c in categories)
                        {
                            xml.WriteStartElement("categoria");

                            writeMethod("ID", c.CategoryID.ToString());
                            writeMethod("Nome", c.CategoryName);
                            writeMethod("Descricao", c.Description);
                            writeMethod("QuantidadeProdutos", c.Products.Count.ToString());

                            xml.WriteStartElement("Produtos");

                            foreach (Product p in c.Products)
                            {
                                xml.WriteStartElement("produto");

                                writeMethod("ID", p.ProductID.ToString());
                                writeMethod("Name", p.ProductName);
                                writeMethod("Custo", p.Cost.Value.ToString());
                                writeMethod("Estoque", p.Stock.ToString());
                                writeMethod("Descontinuado", p.Discontinued.ToString());

                                xml.WriteEndElement();
                            }

                            xml.WriteEndElement();
                            xml.WriteEndElement();
                        }

                        xml.WriteEndElement();
                    }
                }

                WriteLine($"{xmlFile} contém {new FileInfo(xmlFile).Length:N0} bytes");
            }

        }

        static void SerializeInJson()
        {
            using (var db = new Northwind())
            {
                IQueryable<Category> categories = db.Categories.Include(c => c.Products);
                string jsonFile = "categories_products.json";

                using (FileStream jsonStream = File.Create(Combine(CurrentDirectory, jsonFile)))
                {
                    using (var json = new Utf8JsonWriter(jsonStream, new JsonWriterOptions { Indented = true }))
                    {
                        json.WriteStartObject();
                        json.WriteStartArray("categorias");

                        foreach (Category c in categories)
                        {
                            json.WriteStartObject();

                            json.WriteNumber("ID", c.CategoryID);
                            json.WriteString("Nome", c.CategoryName);
                            json.WriteString("Desc", c.Description);
                            json.WriteNumber("ProdutoQuant", c.Products.Count);

                            json.WriteStartArray("produtos");

                            foreach (Product p in c.Products)
                            {
                                json.WriteStartObject();

                                json.WriteNumber("ID", p.ProductID);
                                json.WriteString("Nome",p.ProductName);
                                json.WriteNumber("Custo", p.Cost.Value);
                                json.WriteNumber("Estoque", p.Stock.Value);
                                json.WriteBoolean("Descontinuado", p.Discontinued);

                                json.WriteEndObject();
                            }

                            json.WriteEndArray();
                            json.WriteEndObject();
                        }
                        json.WriteEndArray();
                        json.WriteEndObject();

                    }
                }
                Console.WriteLine($"{jsonFile} contém {new FileInfo(jsonFile).Length:N0} bytes");
            }
        }

        [Obsolete]
        static void SerializeInBinary()
        {
            using (var db = new Northwind())
            {
                IQueryable<Category> categories = db.Categories.Include(c => c.Products);

                BinaryFormatter bf = new BinaryFormatter();
                FileStream fileStream;
                if(File.Exists(CurrentDirectory + "/categories_products.dat")) File.Delete(CurrentDirectory + "/categories_products.dat");
                fileStream = File.Create(CurrentDirectory + "/categories_products.dat");
                foreach (Category c in categories)
                {
                    foreach (Product p in c.Products)
                    {
                        bf.Serialize(fileStream, p);
                    }
                }

                fileStream.Close();
                Console.WriteLine($"{"categories_products.dat"} contém {new FileInfo("categories_products.dat").Length:N0} bytes");
            }

        }
        static void ListProducts()
        {
            using (var db = new Northwind())
            {
                WriteLine("{0,-3} {1,-35} {2,8} {3,5} {4}",
                "ID", "Nome", "Custo", "Estoque", "Descon.");
                foreach (var item in db.Products.OrderByDescending(p => p.Cost))
                {
                    WriteLine("{0:000} {1,-35} {2,8:$#,##0.00} {3,5} {4}",
                    item.ProductID, item.ProductName, item.Cost,
                    item.Stock, item.Discontinued);
                }
            }
        }
        static bool IncreaseProductPrice(string name, decimal amount)
        {
            using (var db = new Northwind())
            {
                Product updateProduct = db.Products.First(
                p => p.ProductName.StartsWith(name));

                updateProduct.Cost += amount;
                int affected = db.SaveChanges();
                return (affected == 1);
            }
        }

        static int DeleteProducts(string name)
        {
            using (var db = new Northwind())
            {
                using (IDbContextTransaction t = db.Database.BeginTransaction())
                {
                    WriteLine("Nível de isolamento da transação: {0}", t.GetDbTransaction().IsolationLevel);
                    var products = db.Products.Where(
                    p => p.ProductName.StartsWith(name));

                    db.Products.RemoveRange(products);

                    int affected = db.SaveChanges();
                    t.Commit();
                    return affected;
                }
            }
        }
        
        [Obsolete]
        static void Main(string[] args)
        {
            // QueryingCategories();
            // FilteredIncludes();
            // QueryingProducts();
            // QueryingWithLike();

            /* if (AddProduct(6, "Bob's Burgers", 500M) WriteLine("Produto adicionado com sucesso.");
             ListProducts();
            */

            /* if (IncreaseProductPrice("Bob", 20M)) WriteLine("Produto atualizado com sucesso");
            ListProducts();
            */

            /*
            int deleted = DeleteProducts("Bob");
            WriteLine($"{deleted} produtos(s) foram deletados");
            */

            SerializeInCsv();
            SerializeInBinary();
            SerializeInXml();
            SerializeInJson();
        }
    }
}
