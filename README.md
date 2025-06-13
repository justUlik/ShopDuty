
# ShopDuty
- OrdersService
- PaymentsService
- ApiGateway
- RabbitMQ
- PostgreSQL (orders-db, payments-db)
- Все очереди и обменники создаются автоматически.
- Используются **Outbox Pattern** и **Inbox Pattern** для надежной обработки сообщений.

## Запуск

```bash
docker compose up --build
```
ApiGateway будет по адресу http://localhost:8080/swagger

## Архитектура обмена сообщениями

- Заказ создается → OrdersService публикует `OrderPaymentRequested` в `order-payments` exchange (fanout).
- PaymentsService слушает очередь `payments-service-inbox`, обрабатывает платеж, отправляет `OrderPaymentSucceeded` или `OrderPaymentFailed` в exchange `order-status`.
- OrdersService слушает `payment-events` → обновляет статус заказа.

## Как проверить сценарии

### Тестирование заказа

- **POST** `/orders` → создать заказ
- **GET** `/orders` → проверить наличие заказа
- **GET** `/orders/{orderId}` → проверить конкретный заказ

### Тестирование оплаты

- **POST** `/accounts/topup` → пополнить баланс
- **POST** `/orders` → создать заказ
- → подождать 5-10 сек, система спишет деньги (InboxProcessor + OutboxPublisher + PaymentConsumer)
- **GET** `/accounts/{userId}/balance` → проверить баланс
- **GET** `/orders/{orderId}` → статус → `PAID` или `CANCELLED`
