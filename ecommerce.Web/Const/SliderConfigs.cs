namespace ecommerce.Web.Const;
public static class SliderConfigs
{
    public static readonly object Category1 = new
    {
        arrows = false,
        infinite = true,
        autoplay = true,
        autoplaySpeed = 2000,
        slidesToShow = 7,
        slidesToScroll = 1,
        dots = true,
        responsive = new[]
        {
            new
            {
                breakpoint = 1745,
                settings = new { slidesToShow = 6, dots = true, autoplay = true, autoplaySpeed = 2000 }
            },
            new
            {
                breakpoint = 1540,
                settings = new { slidesToShow = 5, dots = true, autoplay = true, autoplaySpeed = 2000 }
            },
            new
            {
                breakpoint = 910,
                settings = new { slidesToShow = 4, dots = true, autoplay = true, autoplaySpeed = 2000 }
            },
            new
            {
                breakpoint = 730,
                settings = new { slidesToShow = 3, dots = true, autoplay = true, autoplaySpeed = 2000 }
            },
            new
            {
                breakpoint = 410,
                settings = new { slidesToShow = 2, dots = true, autoplay = true, autoplaySpeed = 2000 }
            }
        }
    };

    public static readonly object Category2 = new
    {
        arrows = false,
        infinite = true,
        autoplay = true,
        autoplaySpeed = 2000,
        slidesToShow = 7,
        slidesToScroll = 1,
        responsive = new[]
        {
            new
            {
                breakpoint = 1745,
                settings = new { slidesToShow = 6, dots = true, autoplay = true, autoplaySpeed = 2000 }
            },
            new
            {
                breakpoint = 1540,
                settings = new { slidesToShow = 5, dots = true, autoplay = true, autoplaySpeed = 2000 }
            },
            new
            {
                breakpoint = 910,
                settings = new { slidesToShow = 4, dots = true, autoplay = true, autoplaySpeed = 2000 }
            },
            new
            {
                breakpoint = 730,
                settings = new { slidesToShow = 3, dots = true, autoplay = true, autoplaySpeed = 2000 }
            },
            new
            {
                breakpoint = 410,
                settings = new { slidesToShow = 2, dots = true, autoplay = true, autoplaySpeed = 2000 }
            }
        }
    };
    public static readonly object Slider_7_1 = new
    {
        arrows = false,
        infinite = true,
        autoplay = true,
        autoplaySpeed = 2000,
        slidesToShow = 7,
        slidesToScroll = 1,
        responsive = new[]
        {
            new
            {
                breakpoint = 1745,
                settings = new { slidesToShow = 6, dots = true, autoplay = true, autoplaySpeed = 2000 }
            },
            new
            {
                breakpoint = 1540,
                settings = new { slidesToShow = 5, dots = true, autoplay = true, autoplaySpeed = 2000 }
            },
            new
            {
                breakpoint = 910,
                settings = new { slidesToShow = 4, dots = true, autoplay = true, autoplaySpeed = 2000 }
            },
            new
            {
                breakpoint = 730,
                settings = new { slidesToShow = 3, dots = true, autoplay = true, autoplaySpeed = 2000 }
            },
            new
            {
                breakpoint = 410,
                settings = new { slidesToShow = 2, dots = true, autoplay = true, autoplaySpeed = 2000 }
            }
        }
    };

    public static readonly object Slider4 = new
    {
        arrows = false,
        dots = false,
        
        infinite = true,
        slidesToShow = 4,
        slidesToScroll = 1,
        autoplay = true,
        autoplaySpeed = 2000,
        responsive = new object[]
        {
            new
            {
                breakpoint = 1410,
                settings = new { slidesToShow = 3 }
            },
            new
            {
                breakpoint = 980,
                settings = new { slidesToShow = 2 }
            },
            new
            {
                breakpoint = 576,
                settings = new { slidesToShow = 1, fade = true }
            }
        }
    };

    public static readonly object SliderHomeBanner = new
    {
        arrows = true,
        dots = true,
        infinite = true,
        slidesToShow = 1,
        slidesToScroll = 1,
        autoplay = true,
        autoplaySpeed = 2000,
        fade = true,
        responsive = new object[]
        {
            new
            {
                breakpoint = 992,
                settings = new { slidesToShow = 1, fade = true }
            },
            new
            {
                breakpoint = 576,
                settings = new { slidesToShow = 1, fade = true }
            }
        }
    };

    public static readonly object Slider6 = new
    {
        arrows = false,
        infinite = true,
        slidesToShow = 6,
        slidesToScroll = 1,
        responsive = new object[]
        {
            new
            {
                breakpoint = 1560,
                settings = new { slidesToShow = 5, autoplay = true, autoplaySpeed = 3500 }
            },
            new
            {
                breakpoint = 1270,
                settings = new { slidesToShow = 4 }
            },
            new
            {
                breakpoint = 1010,
                settings = new { slidesToShow = 3 }
            },
            new
            {
                breakpoint = 730,
                settings = new { slidesToShow = 2 }
            }
        }
    };
    public static readonly object ProductMain2 = new
    {
        slidesToShow = 1,
        slidesToScroll = 1,
        arrows = false,
        fade = true,
        asNavFor = ".left-slider-image-2"
    };
    public static readonly object ProductThumbnail2 = new
    {
        slidesToShow = 4,
        slidesToScroll = 1,
        asNavFor = ".product-main-2",
        dots = false,
        focusOnSelect = true,
        vertical = true,
        responsive = new object[]
        {
            new
            {
                breakpoint = 1400,
                settings = new
                {
                    vertical = false
                }
            },
            new
            {
                breakpoint = 992,
                settings = new
                {
                    vertical = true
                }
            },
            new
            {
                breakpoint = 768,
                settings = new
                {
                    vertical = false
                }
            },
            new
            {
                breakpoint = 430,
                settings = new
                {
                    slidesToShow = 3,
                    vertical = false
                }
            }
        }
    };

    public static readonly object Slider6_1 = new
    {
        arrows = false,
        infinite = true,
        slidesToShow = 6,
        slidesToScroll = 1,
        dots = true,
        responsive = new object[]
        {
            new
            {
                breakpoint = 1430,
                settings = new
                {
                    slidesToShow = 5,
                    autoplay = true,
                    autoplaySpeed = 3500
                }
            },
            new
            {
                breakpoint = 1199,
                settings = new
                {
                    slidesToShow = 4
                }
            },
            new
            {
                breakpoint = 768,
                settings = new
                {
                    slidesToShow = 3
                }
            },
            new
            {
                breakpoint = 600,
                settings = new
                {
                    slidesToShow = 2
                }
            }
        }
    };
    public static readonly object Slider_3_1 = new
    {
        infinite = true,
        slidesToScroll = 1,
        slidesToShow = 3,
        arrows = false,
        responsive = new object[]
        {
            new
            {
                breakpoint = 1262,
                settings = new
                {
                    slidesToShow = 2,
                    autoplay = true,
                    autoplaySpeed = 2800,
                    dots = true
                }
            },
            new
            {
                breakpoint = 650,
                settings = new
                {
                    slidesToShow = 1,
                    dots = true,
                    autoplay = true,
                    autoplaySpeed = 2800
                }
            }
        }
    };
    public static readonly dynamic SliderNotification = new
    {
        slidesToShow = 1,
        slidesToScroll = 1,
        dots = false,
        vertical = true,
        variableWidth = false,
        autoplay = true,
        autoplaySpeed = 2000,
        arrows = false
    };
    public static readonly dynamic Slider4_Responsive = new
    {
        arrows = false,
        infinite = true,
        slidesToShow = 4,
        slidesToScroll = 1,
        autoplay = true,
        autoplaySpeed = 2000,
        responsive = new object[]
        {
            new
            {
                breakpoint = 992,
                settings = new { slidesToShow = 3, autoplay = true, autoplaySpeed = 2000, dots = true }
            },
            new
            {
                breakpoint = 768,
                settings = new { slidesToShow = 2, autoplay = true, autoplaySpeed = 2000, dots = true }
            },
            new
            {
                breakpoint = 474,
                settings = new { slidesToShow = 1, autoplay = true, autoplaySpeed = 2000, dots = true }
            }
        }
    };
    public const string AddToCartScript = "$('.addcart-button').click(function () { $(this).next().addClass('open'); $('.add-to-cart-box .qty-input').val('1'); });";
    public const string IncrementQtyScript = "$('.add-to-cart-box').on('click', function () { var $qty = $(this).siblings('.qty-input'); var currentVal = parseInt($qty.val()); if (!isNaN(currentVal)) { $qty.val(currentVal + 1); } });";
}
