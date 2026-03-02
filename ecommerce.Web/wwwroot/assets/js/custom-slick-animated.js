 /**=====================
     Custom Slick Animated js
==========================**/
document.addEventListener('DOMContentLoaded', function(){
  var init = function(){
    if (window.$ && $('.slider-animate').length) {
      $('.slider-animate').slick({
          autoplay: true,
          speed: 1800,
          lazyLoad: 'progressive',
          fade: true,
          dots: true,
      }).slickAnimation();
    }
  };
  if ('requestIdleCallback' in window) {
    requestIdleCallback(init);
  } else {
    setTimeout(init, 0);
  }
});