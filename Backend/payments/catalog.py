from dataclasses import dataclass


@dataclass(frozen=True)
class IapProduct:
    product_id: str
    coins: int
    price_inr: int
    display_name: str


# Base rate: ₹10 = 100 coins (10 coins per rupee).
# Product IDs must match Google Play Console in-app products.
IAP_PRODUCTS: dict[str, IapProduct] = {
    "coins_100": IapProduct("coins_100", 100, 10, "100 Coins"),
    "coins_500": IapProduct("coins_500", 500, 50, "500 Coins"),
    "coins_1000": IapProduct("coins_1000", 1000, 100, "1,000 Coins"),
    "coins_2500": IapProduct("coins_2500", 2500, 250, "2,500 Coins"),
    "coins_5000": IapProduct("coins_5000", 5000, 500, "5,000 Coins"),
}


def get_product(product_id: str) -> IapProduct | None:
    return IAP_PRODUCTS.get(product_id)
