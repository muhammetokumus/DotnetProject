using ASPUI.Models;
using Business.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace ASPUI.Controllers
{
    public class HomeController : Controller
    {
        ICategoryService categoryService;
        IProductService productService;

        public HomeController(ICategoryService categoryService, IProductService productService)
        {
            this.categoryService = categoryService;
            this.productService = productService;
        }

        public IActionResult Index(int id)
        {
            HomeViewModel model = new HomeViewModel();
            if (id == 0)
            {
                model.products = productService.GetAll();
            }
            else
            {
                model.products = productService.GetAll(id);
            }
            model.categories = categoryService.GetAll();
            return View(model);
        }

        [HttpPost]
        public IActionResult Index(string description)
        {
            HomeViewModel model = new HomeViewModel();
            model.products = productService.GetAllByDescription(description);
            model.categories = categoryService.GetAll();
            return View(model);
        }
        public IActionResult Details(int id)
        {
            var product = productService.GetById(id);
            return View(product);
        }
    }
}