# Bezier Render System

There is simple quadratic bezier(**Bézier**) curve render system. This system supports to draw and fill quadratic bezier curve.

The core code of rendering is `BezierRender` using `C#` and `Direct3D11`.

See more [Click](https://linkclinton.com/index.php/2019/05/14/bezier-render-system/).

## Framework

This code is based on my framework(`GalEngine`). But it is easy to understand or translate to other framework.

## Basic Idea

### Draw

We use the equation `Q'(s) * (P - Q(s)) = 0` to detect if one pixel is inside the curve with width. `Q` is bezier curve, `P` is the position of pixel. `Q'` is the derivative of `Q`. If there is any root of equation and the root is vaild(`s` in [0, 1] and distance(`Q(s)`, `P`) less than half width), the pixel is inside.

See more in [Mark J. Kilgard and Jeff Bolz, GPU-accelerated Path Rendering. ACM Transactions on Graphics, 31(6) : 2012.]

### Fill

We use this formula `f(u, v) = u * u - v > 0` to detect if one pixel is inside the bezier curve. If not, we discard this pixel. Otherwise we render it. 

See more in [Resolution Independent Curve Rendering using Programmable Graphics Hardware, Charles Loop, Jim Blinn,  ACM SIGGRAPH 2005 Papers, SIGGRAPH ’05, 1000–1009, 2005
].

## MSAA

There is another way to render(fill) with MSAA. We do not need to expand the frame buffer. And we can only sample edge-pixel(pixel where edge cross) with MSAA. But we must to transform vertex in CPU(We can use RWBuffer to transform it with GPU).

For each bezier, we need to record the transformed points(in render target coordinate system) of triangle we render.

For each pixel, we can choose 4(for 4x MSAA) sample and compute their position in render target coordinate system. So we can use `barycentric coordinates` to compute the `(u, v)` of sample. Finally, we can add `alpha` for sample who inside the curve and divide it by 4(for 4x MSAA). The result `alpha` is pixel's `alpha` and we use it to blend.

The pixel inside curve and has far distance to edge of curve, it's `f(u, v)` must more than pixel who near edge of curve. So we can ignore the pixel whose `abs(f(u, v))` more than `x`(x is any float number we choose).

As for drawing, We can multi-sample it only the pixel inside the curve with width.

## Performance

I had not compare it with other way like `Direct2D`. But there are some data of this system in my machine.

| Mode              | Native     | 4XMSAA     | 4XEMSAA   | NativeI   | 4XMSAAI   | 4XEMSAAI  |
| ----              | ------     | ------     | ------    | -------   | -------   | --------  |
| FPS(Draw)         | 26FPS      | 19FPS      | 20FPS     | 92FPS     | 41FPS     | 72FPS     |
| FPS(Fill)         | 33FPS      | 24FPS      | 24FPS     | 122FPS    | 115FPS    | 122FPS
| Number of Beziers | 4000       | 4000       | 4000      | 4000      | 4000      | 4000      |
| Resolution        | 1920x1080  | 1920x1080  | 1920x1080 | 1920x1080 | 1920x1080 | 1920x1080 |
| CPU               | i7-8650U    | i7-8650U    | i7-8650U   | i7-8650U   | i7-8650U   | i7-8650U   |
| GPU               | GTX 1060 | GTX 1060 | GTX 1060 | GTX 1060 | GTX 1060| GTX 1060   |

- Native : Simple Rendering.
- 4XMSAA : 4XMSAA for all pixel.
- 4XEMSAA : 4XMSAA for edge pixel(inside pixel).
- NativeI : Simple Rendering with Instance.
- 4XMSAAI : 4XMSAA for all pixel with Instance.
- 4XEMSAAI : 4XMSAA for edge pixel(inside pixel) with Instance.

**For NativeI, 4XMSAAI, 4XEMSAAI, the bottleneck is CPU instead of GPU. I do not support multi-thread or orther way for maximizing CPU utilization, but I think it is feasible.**

## Example

There are some photos I render. I render 500 flower(4000 bezier curves) with 1080P.

**This photo is drawing with 4XEMSAA.**

![draw_4xemsaa](https://linkclinton.com/wp-content/uploads/2019/05/draw_4xemsaa.png)

**This photo is drawing without MSAA.**

![draw_native](https://linkclinton.com/wp-content/uploads/2019/05/draw_native.png)

**This photo is filling with 4XEMSAA.**

![fill_4xemsaa](https://linkclinton.com/wp-content/uploads/2019/05/fill_4xemsaa.png)

**This photo is filling without MSAA.**

![fill_native](https://linkclinton.com/wp-content/uploads/2019/05/fill_native.png)