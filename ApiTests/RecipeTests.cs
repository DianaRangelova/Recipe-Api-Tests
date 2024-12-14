using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Net;

namespace ApiTests
{
    [TestFixture]
    public class RecipeTests : IDisposable
    {
        private RestClient client;
        private string token;
        private Random random;
        private string title;

        [SetUp]
        public void Setup()
        {
            client = new RestClient(GlobalConstants.BaseUrl);
            token = GlobalConstants.AuthenticateUser("john.doe@example.com", "password123");

            Assert.That(token, Is.Not.Null.Or.Empty, 
                "Authentication token should not be null or empty");

            random = new Random();
        }

        [Test, Order(1)]
        public void Test_GetAllRecipes()
        {
            // Arrange
            var request = new RestRequest("/recipe", Method.Get);

            // Act
            var response = client.Execute(request);

            // Asserts
            Assert.Multiple(() =>
            {
                // Response Assertions
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The response does not contain the correct status code OK (200)");
                Assert.That(response.Content, Is.Not.Null.Or.Empty, 
                    "Response createdRecepie is not as expected");

                // Data Structure Assertions
                var recipes = JArray.Parse(response.Content);

                Assert.That(recipes.Type, Is.EqualTo(JTokenType.Array), 
                    "Expected response createdRecepie is not a JSON array");
                Assert.That(recipes.Count, Is.GreaterThan(0), 
                    "Recepies count is below 1");

                // Recipe Fields Assertions (for each recipe)
                foreach (var recipe in recipes)
                {
                    Assert.That(recipe["title"]?.ToString(), Is.Not.Null.Or.Empty, 
                        "Property is not as expected");
                    Assert.That(recipe["ingredients"], Is.Not.Null.Or.Empty,
                        "Property is not as expected");
                    Assert.That(recipe["instructions"], Is.Not.Null.Or.Empty,
                        "Property is not as expected");
                    Assert.That(recipe["cookingTime"], Is.Not.Null.Or.Empty,
                        "Property is not as expected");
                    Assert.That(recipe["servings"], Is.Not.Null.Or.Empty,
                        "Property is not as expected");
                    Assert.That(recipe["category"], Is.Not.Null.Or.Empty,
                        "Property is not as expected");

                    Assert.That(recipe["ingredients"]?.Type, Is.EqualTo(JTokenType.Array),
                       "Recipe ingreients are not a JSON Array");
                    Assert.That(recipe["instructions"]?.Type, Is.EqualTo(JTokenType.Array), 
                        "Recipe instructions are not a JSON Array");
                }
            });
        }

        [Test, Order(2)]
        public void Test_GetRecipeByTitle()
        {
            // Get request for all recepies
            var expectedCookingTime = 25;
            var expectedServings = 24;
            var expectedIngredients = 9;
            var expectedInstructions = 7;
            var titleToGet = "Chocolate Chip Cookies";
            var getRequest = new RestRequest("/recipe", Method.Get);

            // Act
            var response = client.Execute(getRequest);

            // Assert
            Assert.Multiple(() =>
            {
                // Response Assertions
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The response does not contain the correct status code OK (200)");
                Assert.That(response.Content, Is.Not.Null.Or.Empty,
                    "Response createdRecepie is not as expected");

                // Data Structure Assertions
                var recipes = JArray.Parse(response.Content);
                var recipe = recipes.FirstOrDefault(r => r 
                ["title"]?.ToString() == titleToGet);

                Assert.That(recipe, Is.Not.Null, 
                    $"Recipe with title {titleToGet} not found");

                // Recipe Fields Assertions
                Assert.That(recipe["cookingTime"].Value<int>(), Is.EqualTo(expectedCookingTime), 
                    "Property does not match the expected value");
                Assert.That(recipe["servings"].Value<int>(), Is.EqualTo(expectedServings),
                    "Property does not match the expected value");
                Assert.That(recipe["ingredients"].Count(), Is.EqualTo(expectedIngredients),
                    "Property does not match the expected value");
                Assert.That(recipe["instructions"].Count(), Is.EqualTo(expectedInstructions),
                    "Property does not match the expected value");
            });
        }

        [Test, Order(3)]
        public void Test_AddRecipe()
        {
            // Arrange
            // Get all categories
            var getCategoriesRequest = new RestRequest("/category", Method.Get);
            var getCategoriesResponse = client.Execute(getCategoriesRequest);

            // Assert
            Assert.Multiple(() =>
            {
                // Response Assertions
                Assert.That(getCategoriesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The response does not contain the correct status code OK (200)");
                Assert.That(getCategoriesResponse.Content, Is.Not.Null.Or.Empty, 
                    "Categories response createdRecepie is not as expected");
            });

            var categories = JArray.Parse(getCategoriesResponse.Content);

            // Extract the first category id
            var categoryId = categories.First()["_id"]?.ToString();

            // Create a request for creating recepie
            var createRecepieRequest = new RestRequest("/recipe", Method.Post);
            createRecepieRequest.AddHeader("Authorization", $"Bearer {token}");

            title = $"recepieTitle_{random.Next(999, 9999)}";
            var cookingTime = 50;
            var servings = 4;
            var ingredients = new[] { new { name = "Test Ingredient", quantity = "10g" } };
            var instructions = new[] { new { step = "Test Step" } };

            createRecepieRequest.AddJsonBody(new
            {
                title = title,
                cookingTime,
                servings,
                ingredients,
                instructions,
                category = categoryId
            });

            // Act
            var addResponse = client.Execute(createRecepieRequest);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(addResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The response does not contain the correct status code OK (200)");
                Assert.That(addResponse.Content, Is.Not.Empty,
                    "Response createdRecepie is not as expected");
            });

            // Get the details of the Recipe
            var createdRecipe = JObject.Parse(addResponse.Content);
            var createdRecipeId = createdRecipe["_id"]?.ToString();

            // Get request for getting by id
            var getByIdRequest = new RestRequest($"/recipe/{createdRecipeId}", Method.Get);
            var getByIdResponse = client.Execute(getByIdRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getByIdResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The response does not contain the correct status code OK (200)");
                Assert.That(getByIdResponse.Content, Is.Not.Empty,
                    "Response content is not as expected");

                var createdRecepie = JObject.Parse(getByIdResponse.Content);

                // Recipe Fields Assertions
                Assert.That(createdRecepie["title"]?.ToString(), Is.EqualTo(title), 
                    "Recipe title does not match the input");
                Assert.That(createdRecepie["cookingTime"]?.Value<int>(), Is.EqualTo(cookingTime),
                    "Recipe cookingTime does not match the input");
                Assert.That(createdRecepie["servings"]?.Value<int>(), Is.EqualTo(servings),
                    "Recipe servings does not match the input");
                Assert.That(createdRecepie["category"]?["_id"]?.ToString(), Is.EqualTo(categoryId),
                    "Recipe category does not match the input");
                Assert.That(createdRecepie["ingredients"]?.Type, Is.EqualTo(JTokenType.Array),
                    "Recipe ingredients does not match the input");
                
                // Asserts for ingredients
                Assert.That(createdRecepie["ingredients"]?.Count(), Is.EqualTo(ingredients.Count()), 
                    "Recipe ingredients should have the correct number of elements");
                Assert.That(createdRecepie["ingredients"]?[0]["name"]?.ToString(), 
                    Is.EqualTo(ingredients[0].name), "Ingredient name did not match");
                Assert.That(createdRecepie["ingredients"]?[0]["quantity"]?.ToString(), 
                    Is.EqualTo(ingredients[0].quantity), "Ingredient quantity did not match");

                // Asserts for instructions
                Assert.That(createdRecepie["instructions"]?.Type, Is.EqualTo(JTokenType.Array), 
                    "Recipe instructions should be a JSON Array");
                Assert.That(createdRecepie["instructions"]?[0]["step"]?.ToString(),
                    Is.EqualTo(instructions[0].step), "Recipe instructions should have the correct number of elements");
            });
        }

        [Test, Order(4)]
        public void Test_UpdateRecipe()
        {
            // Arrange
            // Get by title
            var getRequest = new RestRequest("/recipe", Method.Get);

            var getResponse = client.Execute(getRequest);

            // Assert
            Assert.Multiple(() =>
            {
                // Response Assertions
                Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The response does not contain the correct status code OK (200)");
                Assert.That(getResponse.Content, Is.Not.Null.Or.Empty,
                    "Response createdRecepie is not as expected");
            });

            var recipes = JArray.Parse(getResponse.Content);
            var recipe = recipes.FirstOrDefault(r => r 
            ["title"]?.ToString() == title);

            Assert.That(recipe, Is.Not.Null,
                $"Recipe with title {title} not found");

            // Get the id of the Recipe
            var recipeId = recipe["_id"].ToString();

            // Create update request
            var updateRequest = new RestRequest($"/recipe/{recipeId}", Method.Put);
            updateRequest.AddHeader("Authorization", $"Bearer {token}");
            title = title + "_udpated";
            var updatedServings = 30;
            updateRequest.AddJsonBody(new
            {
                title = title,
                servings = updatedServings
            });

            // Act
            var updateResponse = client.Execute(updateRequest);

            // Assert
            Assert.Multiple(() =>
            {
                // Update Response Assertions
                Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The response does not contain the correct status code OK (200)");
                Assert.That(updateResponse.Content, Is.Not.Null.Or.Empty,
                    "Response createdRecepie is not as expected");

                var updatedRecepie = JObject.Parse(updateResponse.Content);

                // Update Recipe Fields Assertions
                Assert.That(updatedRecepie["title"]?.ToString(), Is.EqualTo(title), 
                    "Recipe title does not match the updated value");
                Assert.That(updatedRecepie["servings"]?.Value<int>(), Is.EqualTo(updatedServings),
                    "Recipe servings does not match the updated value");
            });

        }

        [Test, Order(5)]
        public void Test_DeleteRecipe()
        {
            // Arrange
            // Get by title
            var getRequest = new RestRequest("/recipe", Method.Get);

            var getResponse = client.Execute(getRequest);

            // Assert
            Assert.Multiple(() =>
            {
                // Response Assertions
                Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The response does not contain the correct status code OK (200)");
                Assert.That(getResponse.Content, Is.Not.Null.Or.Empty,
                    "Response createdRecepie is not as expected");
            });

            var recipes = JArray.Parse(getResponse.Content);
            var recipe = recipes.FirstOrDefault(r => r
            ["title"]?.ToString() == title);

            Assert.That(recipe, Is.Not.Null,
                $"Recipe with title {title} not found");

            // Get the id of the Recipe
            var recipeId = recipe["_id"].ToString();

            // Create delete request
            var deleteRequest = new RestRequest($"/recipe/{recipeId}", Method.Delete);
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");

            // Act
            var deleteResponse = client.Execute(deleteRequest);

            // Assert
            // Post-Deletion Verification
            Assert.Multiple(() =>
            {
                Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The response does not contain the correct status code OK (200)");

                // Get request by id
                var verifyGetRequest = new RestRequest($"/recipe/{recipeId}", Method.Get);

                var verifyGetResponse = client.Execute(verifyGetRequest);

                Assert.That(verifyGetResponse.Content, Is.EqualTo("null"), 
                    "Verify get response content should be empty");
            });
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
