namespace Application.Request.OrderReq
{
    public class CreateOrderDetailRequest
    {
        public int ItemVariantId { get; set; }
        public int Quantity { get; set; }
    }
}