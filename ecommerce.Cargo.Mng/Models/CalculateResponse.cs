namespace ecommerce.Cargo.Mng.Models;

public class CalculateResponse
{
    public double TransferRemuneration { get; set; }

    public double PrivateServicePrice { get; set; }

    public double PickUpPrice { get; set; }

    public double DeliveryPrice { get; set; }

    public double SmsPrice { get; set; }

    public double InsurancePrice { get; set; }

    public double PostPrice { get; set; }

    public double SubTotal { get; set; }

    public double Kdv { get; set; }

    public double FinalTotal { get; set; }
}