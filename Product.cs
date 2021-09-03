using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable()]
public class Product : ISerializable
{
    public int ProductID { get; set; }
    [Required]
    [StringLength(40)]

    public string ProductName { get; set; }

    [Column("UnitPrice", TypeName = "money")]
    public decimal? Cost { get; set; }
    
    [Column("UnitsInStock")]
    public short? Stock { get; set; }

    public bool Discontinued { get; set; }

    public int CategoryID { get; set; }

    public virtual Category Category { get; set; }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("Nome", ProductName);
        info.AddValue("ID", ProductID);
        info.AddValue("Custo", Cost);
        info.AddValue("Estoque", Stock);
        info.AddValue("Descontinuado", Discontinued);
        info.AddValue("IDcategoria", CategoryID);
    }

    public Product(SerializationInfo info, StreamingContext context)
    {
        ProductName = (string) info.GetValue("Nome", typeof(string));
        ProductID = (int) info.GetValue("ID", typeof(int));
        Cost = (decimal) info.GetValue("Custo", typeof(decimal));
        Stock = (short) info.GetValue("Estoque", typeof(short));
        Discontinued = (bool) info.GetValue("Descontinuado", typeof(bool));
        CategoryID = (int) info.GetValue("IDcategoria", typeof(int));
 }

    public Product(){

    }
}