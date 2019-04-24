﻿using JsonUtils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sales.ViewModelComposition.Events;
using ServiceComposer.AspNetCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sales.ViewModelComposition
{
    class ShoppingCartGetHandler : IHandleRequests
    {
        public bool Matches(RouteData routeData, string httpVerb, HttpRequest request)
        {
            var controller = (string)routeData.Values["controller"];
            var action = (string)routeData.Values["action"];

            return HttpMethods.IsGet(httpVerb)
                   && controller.ToLowerInvariant() == "shoppingcart"
                   && action.ToLowerInvariant() == "index"
                   && !routeData.Values.ContainsKey("id");
        }

        public async Task Handle(string requestId, dynamic vm, RouteData routeData, HttpRequest request)
        {
            var id = request.Cookies["cart-id"];

            var url = $"http://localhost:5001/api/shopping-cart/{id}";
            var client = new HttpClient();
            var response = await client.GetAsync(url);

            dynamic shoppingCart = await response.Content.AsExpando();

            if (shoppingCart.Items.Count == 0)
            {
                vm.CartId = id;
                vm.CartItems = new List<dynamic>();

                return;
            }

            IDictionary<dynamic, dynamic> cartItemsViewModel = MapToDictionary(shoppingCart.Items);

            await vm.RaiseEvent(new ShoppingCartItemsLoaded()
            {
                CartId = new Guid((string)shoppingCart.CartId),
                CartItemsViewModel = cartItemsViewModel
            }).ConfigureAwait(false);

            vm.CartId = shoppingCart.CartId;
            vm.CartItems = cartItemsViewModel.Values.ToList();
        }

        IDictionary<dynamic, dynamic> MapToDictionary(IEnumerable<object> cartItems)
        {
            var cartItemsViewModel = new Dictionary<dynamic, dynamic>();

            foreach (dynamic item in cartItems)
            {
                dynamic vm = new ExpandoObject();

                vm.ProductId = item.ProductId;
                vm.CurrentPrice = item.CurrentPrice;
                vm.LastPrice = item.LastPrice;
                vm.IsPriceChanged = item.CurrentPrice != item.LastPrice;
                vm.Quantity = item.Quantity;
                vm.TotalPrice = item.CurrentPrice * item.Quantity;

                cartItemsViewModel[item.ProductId] = vm;
            }

            return cartItemsViewModel;
        }
    }
}
