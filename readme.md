# Bezier Render System

There is simple quadratic bezier(**Bézier**) curve render system.

The core code of rendering is `BezierRender` using `C#` and `Direct3D11`.

## Framework

This code is based on my framework(`GalEngine`). But it is easy to understand or translate to other framework.

## Basic Idea

We use this formula `f(u, v) = u * u - v > 0` to detect if one pixel is inside the bezier curve. If not, we discard this pixel. Otherwise we render it. 

See more in [Resolution Independent Curve Rendering using Programmable Graphics Hardware, Charles Loop, Jim Blinn,  ACM SIGGRAPH 2005 Papers, SIGGRAPH ’05, 1000–1009, 2005
].

## MSAA

There is another way to render with MSAA. We do not need to expand the frame buffer. And we can only sample edge-pixel(pixel where edge cross) with MSAA. But we must to transform vertex in CPU(We can use RWBuffer to transform it with GPU).

For each bezier, we need to record the transformed points(in render target coordinate system) of triangle we render.

For each pixel, we can choose 4(for 4x MSAA) sample and compute their position in render target coordinate system. So we can use `barycentric coordinates` to compute the `(u, v)` of sample. Finally, we can add `alpha` for sample who inside the curve and divide it by 4(for 4x MSAA). The result `alpha` is pixel's `alpha` and we use it to blend.

The pixel inside curve and has far distance to edge of curve, it's `f(u, v)` must more than pixel who near edge of curve. So we can ignore the pixel whose `abs(f(u, v))` more than `x`(x is any float number we choose).

## Performance

I had not compare it with other way like `Direct2D`. But there are some data of this system.

| Mode              | Native     | 4XMSAA     | 4XEMSAA   | NativeI   | 4XMSAAI   | 4XEMSAAI  |
| ----              | ------     | ------     | ------    | -------   | -------   | --------  |
| FPS               | 30FPS      | 25FPS      | 28FPS     | 95FPS     | 40FPS     | 80FPS     |
| Number of Beziers | 4000       | 4000       | 4000      | 4000      | 4000      | 4000      |
| Resolution        | 1920x1080  | 1920x1080  | 1920x1080 | 1920x1080 | 1920x1080 | 1920x1080 |
| CPU               | m3-6y30    | m3-6y30    | m3-6y30   | m3-6y30   | m3-6y30   | m3-6y30   |
| GPU               | Grpahics 515 | Grpahics 515 | Grpahics 515 | Grpahics 515 | Grpahics 515| Grpahics 515   |

- Native : Simple Rendering.
- 4XMSAA : 4XMSAA for all pixel.
- 4XEMSAA : 4XMSAA for edge pixel.
- NativeI : Simple Rendering with Instance.
- 4XMSAAI : 4XMSAA for all pixel with Instance.
- 4XEMSAAI : 4XMSAA for edge pixel with Instance.

## Example

There are some photos I render. I render 500 flower(4000 bezier curves) with 1080P.

![native](https://linkclinton.com/wp-content/uploads/2019/05/native.png)

There is native mode, without MSAA.

![msaa_all](https://linkclinton.com/wp-content/uploads/2019/05/msaa_all.png)

There is 4x MSAA mode and we use the average of sample as output(for outside sample, we use (0, 0, 0, 0)). It is easy to find there are some black edge at edge of curve.

![msaa_all_alpha](https://linkclinton.com/wp-content/uploads/2019/05/msaa_all_alpha.png)

There is 4x MSAA mode and we use the average alpha of sample to blend. It is better than 2nd photo.

![msaa_alpha](https://linkclinton.com/wp-content/uploads/2019/05/msaa_alpha.png)

There is 4x MSAA mode and we only sample pixel near the edge. It is similar to 3rd photo. But the performance is 2 times than 3rd.

