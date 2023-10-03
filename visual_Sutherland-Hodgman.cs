// @COMPILECMD mcs $@ -out:$*.exe -r:System.Windows.Forms.dll -r:System.Drawing.dll
using System;
using System.Drawing;
using System.Windows.Forms;

public
static
class Globals {
	public const int MAX_POINTS = 20;
	public static Random randy = new Random();
}

public
struct spacial_t {	// Erre azért van szükség mert a Point .x-e és .y-a privát
	public int x;
	public int y;

	public
	override
	string ToString() {
		return $"{{{x}, {x}}}, ";
	}

	public
	static implicit
	operator Point(spacial_t s) {
		return new Point(s.x, s.y);
	}

    public
	static implicit
	operator PointF(spacial_t s) {
		return new PointF(s.x, s.y);
	}

	public
	static
	Point[] array_to_point_array(spacial_t[] a) {
		Point[] r = new Point[a.Length];
		for (int i = 0; i < a.Length; i++) {
			r[i] = a[i];
		}
		return r;
	}

    public
	static
	spacial_t[] random_triangle(spacial_t min, spacial_t max) {
		return new spacial_t[] {
			new spacial_t {
				x = Globals.randy.Next(min.x, max.y),
				y = Globals.randy.Next(min.y, max.y)
			},
			new spacial_t {
				x = Globals.randy.Next(min.x, max.y),
				y = Globals.randy.Next(min.y, max.y)
			},
			new spacial_t {
				x = Globals.randy.Next(min.x, max.y),
				y = Globals.randy.Next(min.y, max.y)
			}
		};
    }
}

static
class SutherlandHodgman {
	public static
	int x_intersect(spacial_t p1, spacial_t p2,
	                spacial_t p3, spacial_t p4) {
		int num = ((p1.x*p2.y) - (p1.y*p2.x)) * (p3.x-p4.x)
		        - ((p3.x*p4.y) - (p3.y*p4.x)) * (p1.x-p2.x);

		int den = (p1.x - p2.x)
				* (p3.y - p4.y)
				- (p1.y - p2.y)
				* (p3.x - p4.x);

		return num / den;
	}

	public static
	int y_intersect(spacial_t p1, spacial_t p2,
	                spacial_t p3, spacial_t p4) {
		int num = ((p1.x*p2.y) - (p1.y*p2.x)) * (p3.y-p4.y)
		        - ((p3.x*p4.y) - (p3.y*p4.x)) * (p1.y-p2.y);

		int den = (p1.x - p2.x)
				* (p3.y - p4.y)
				- (p1.y - p2.y)
				* (p3.x - p4.x);

			return num / den;
	}

	public static
	void clip(ref spacial_t[] poly_points,
	              spacial_t p1, spacial_t p2) {
		int new_poly_size = 0;
		spacial_t[] new_points = new spacial_t[Globals.MAX_POINTS];

		for (int i = 0; i < poly_points.Length; i++) {
			int k = (i + 1) % poly_points.Length;
			spacial_t p_i = poly_points[i];
			spacial_t p_k = poly_points[k];

			int i_pos = (p2.x - p1.x)
			          * (p_i.y - p1.y)
			          - (p2.y - p1.y)
			          * (p_i.x - p1.x);
			int k_pos = (p2.x - p1.x)
			          * (p_k.y - p1.y)
			          - (p2.y - p1.y)
			          * (p_k.x - p1.x);

			if (i_pos < 0 && k_pos < 0) {
				new_points[new_poly_size++] = p_k;
			} else if (i_pos >= 0 && k_pos < 0) {
				new_points[new_poly_size++] = new spacial_t {
					x = x_intersect(p1, p2, p_i, p_k),
					y = y_intersect(p1, p2, p_i, p_k)
				};
				new_points[new_poly_size++] = p_k;
			} else if (i_pos < 0 && k_pos >= 0) {
				spacial_t intersection = new spacial_t {
					x = x_intersect(p1, p2, p_i, p_k),
					y = y_intersect(p1, p2, p_i, p_k)
				};
				new_points[new_poly_size++] = intersection;
			}
		}
		
		spacial_t[] swap = new spacial_t[new_poly_size];
		for (int i = 0; i < new_poly_size; i++) {
			swap[i] = new_points[i];
		}
		poly_points = swap;
	}

	public static
	spacial_t[] clipSutherlandHodgman(spacial_t[] poly_points,
	                                  spacial_t[] clipper_points) {
		for (int i = 0; i < clipper_points.Length; i++) {
			int k = (i + 1) % clipper_points.Length;

			clip(ref poly_points,
			     clipper_points[i],
			     clipper_points[k]);
		}

		return poly_points;
	}
}

public
class Program : Form {
	Pen polipen = new Pen(Color.Green, 1);
	Pen clippen = new Pen(Color.Red,   4);
	Pen resupen = new Pen(Color.Blue,  2);

	const int triangle_margin =  50;
	const int clipper_margin  = 200;

	spacial_t[] poly_points = new spacial_t[] {
		new spacial_t { x = 100, y = 150 },
		new spacial_t { x = 200, y = 250 },
		new spacial_t { x = 220, y = 100 }
	};

	spacial_t[] clipper_points = new spacial_t[] {
		new spacial_t { x = 150, y = 150 },
		new spacial_t { x = 150, y = 200 },
		new spacial_t { x = 200, y = 200 },
		new spacial_t { x = 200, y = 150 }
	};

	spacial_t[] r_points;

	public
	Program() {
		this.Text = "Polygon Clipper";
		this.Size = new Size(800, 800);

		Button button = new Button();
			button.Text     = "New Triangle";
			button.Location = new System.Drawing.Point(10, 10);
			button.Size     = new System.Drawing.Size(75, 30);
			button.Click   += new EventHandler(fill);
		this.Controls.Add(button);

		this.fill();
	}

	private
	void fill(object s, EventArgs e) {
		this.fill();
		this.Invalidate();
	}

	private
	void fill() {
		poly_points = spacial_t.random_triangle(
			new spacial_t {
				x = triangle_margin,
				y = triangle_margin
			},
			new spacial_t {
				x = this.ClientSize.Width  - triangle_margin,
				y = this.ClientSize.Height - triangle_margin
			}
		);
		clipper_points = new spacial_t[] {
			new spacial_t {
				x = clipper_margin,
				y = clipper_margin
			},
			new spacial_t {
				x =                          clipper_margin,
				y = this.ClientSize.Height - clipper_margin
			},
			new spacial_t {
				x = this.ClientSize.Width  - clipper_margin,
				y = this.ClientSize.Height - clipper_margin
			},
			new spacial_t {
				x = this.ClientSize.Width - clipper_margin,
				y =                         clipper_margin
			},
		};
		r_points = SutherlandHodgman.clipSutherlandHodgman(poly_points, clipper_points);
	}

	protected
	override
	void OnPaint(PaintEventArgs e) {
		base.OnPaint(e);
		Graphics g = e.Graphics;
	
			g.DrawPolygon(this.polipen, spacial_t.array_to_point_array(this.poly_points));
			g.DrawPolygon(this.clippen, spacial_t.array_to_point_array(this.clipper_points));
		try {
			g.DrawPolygon(this.resupen, spacial_t.array_to_point_array(this.r_points));
		} catch (Exception x) { ; }
	}

	public static
	void Main() {
		Application.Run(new Program());
	}
}
