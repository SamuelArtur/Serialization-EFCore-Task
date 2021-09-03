using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable()]
  public class Category : ISerializable
  {
    public int CategoryID { get; set; }
    public string CategoryName { get; set; }

    [Column(TypeName = "ntext")]
    public string Description { get; set; }
    public virtual ICollection<Product> Products { get; set; }

    public Category()
    {
      this.Products = new HashSet<Product>();
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("Nome", CategoryName);
        info.AddValue("ID", CategoryID);
        info.AddValue("Descrição", Description);
    }

    public Category(SerializationInfo info, StreamingContext context)
    {
        CategoryName = (string) info.GetValue("Nome", typeof(string));
        CategoryID = (int) info.GetValue("ID", typeof(int));
        Description = (string) info.GetValue("Descrição", typeof(string));
    }
  }