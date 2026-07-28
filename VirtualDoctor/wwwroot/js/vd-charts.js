/* ============================================================
   VirtualDoctor - lapisan visualisasi D3
   Semua warna dibaca dari CSS custom property agar chart ikut
   berubah saat tema terang/gelap diganti.
   ============================================================ */
(function () {
    "use strict";

    const registry = new Map();   // id -> { type, data, opts }
    let tooltip = null;

    const reduceMotion = () =>
        window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    function css(name, fallback) {
        const v = getComputedStyle(document.documentElement).getPropertyValue(name);
        return (v && v.trim()) || fallback;
    }

    function palette() {
        return {
            ink: css("--vd-text", "#1a2430"),
            muted: css("--vd-text-secondary", "#5c6876"),
            grid: css("--vd-chart-grid", "#e3ded3"),
            surface: css("--vd-surface", "#ffffff"),
            series: [
                css("--vd-chart-1", "#1b4f63"),
                css("--vd-chart-2", "#2f7d73"),
                css("--vd-chart-3", "#c9a86a"),
                css("--vd-chart-4", "#4c79ff"),
                css("--vd-chart-5", "#c7523c"),
                css("--vd-chart-6", "#7a5ea8")
            ],
            heat: [css("--vd-heat-0", "#f0ede7"), css("--vd-heat-1", "#1b4f63")]
        };
    }

    function ensureTooltip() {
        if (tooltip) return tooltip;
        tooltip = document.createElement("div");
        tooltip.className = "vd-chart-tip";
        tooltip.setAttribute("role", "status");
        document.body.appendChild(tooltip);
        return tooltip;
    }

    function showTip(html, x, y) {
        const t = ensureTooltip();
        t.innerHTML = html;
        t.classList.add("is-visible");
        const rect = t.getBoundingClientRect();
        let left = x + 14;
        if (left + rect.width > window.innerWidth - 8) left = x - rect.width - 14;
        t.style.left = left + "px";
        t.style.top = Math.max(8, y - rect.height - 10) + "px";
    }

    function hideTip() {
        if (tooltip) tooltip.classList.remove("is-visible");
    }

    const fmtInt = (n) => new Intl.NumberFormat("id-ID").format(Math.round(n));
    const fmtMoney = (n) => "Rp" + new Intl.NumberFormat("id-ID", { maximumFractionDigits: 0 }).format(Math.round(n));
    const fmtCompact = (n) => {
        if (Math.abs(n) >= 1e9) return (n / 1e9).toFixed(1).replace(".0", "") + " M";
        if (Math.abs(n) >= 1e6) return (n / 1e6).toFixed(1).replace(".0", "") + " jt";
        if (Math.abs(n) >= 1e3) return (n / 1e3).toFixed(1).replace(".0", "") + " rb";
        return fmtInt(n);
    };
    const fmtDate = (d) => d.toLocaleDateString("id-ID", { day: "2-digit", month: "short" });

    function frame(el, minH) {
        const box = el.getBoundingClientRect();
        return {
            w: Math.max(160, box.width),
            h: Math.max(minH ?? 80, box.height || Number(el.dataset.height) || 220)
        };
    }

    function clear(el) {
        d3.select(el).selectAll("*").remove();
    }

    // ------------------------------------------------------------
    // Sparkline gaya monitor vital - tanda tangan visual dashboard
    // ------------------------------------------------------------
    function ecgSparkline(el, values, opts) {
        const p = palette();
        // sparkline mengisi tinggi wadahnya apa adanya, jangan dipaksa minimum
        const { w, h } = frame(el, 28);
        const color = opts.color || p.series[0];
        const data = (values && values.length ? values : [0, 0]).map(Number);

        clear(el);
        const svg = d3.select(el).append("svg")
            .attr("width", w).attr("height", h)
            .attr("viewBox", `0 0 ${w} ${h}`)
            .attr("preserveAspectRatio", "none")
            .attr("aria-hidden", "true");

        const x = d3.scaleLinear().domain([0, data.length - 1]).range([2, w - 8]);
        const max = d3.max(data) || 1;
        const min = Math.min(0, d3.min(data));
        const y = d3.scaleLinear().domain([min, max === min ? max + 1 : max]).range([h - 6, 6]);

        const id = "vdgrad" + Math.random().toString(36).slice(2, 8);
        const grad = svg.append("defs").append("linearGradient")
            .attr("id", id).attr("x1", 0).attr("y1", 0).attr("x2", 0).attr("y2", 1);
        grad.append("stop").attr("offset", "0%").attr("stop-color", color).attr("stop-opacity", 0.32);
        grad.append("stop").attr("offset", "100%").attr("stop-color", color).attr("stop-opacity", 0);

        // garis dasar seperti pada monitor pasien
        svg.append("line")
            .attr("x1", 0).attr("x2", w).attr("y1", y(min)).attr("y2", y(min))
            .attr("stroke", p.grid).attr("stroke-width", 1).attr("stroke-dasharray", "2 4");

        const area = d3.area().x((_, i) => x(i)).y0(y(min)).y1((d) => y(d)).curve(d3.curveLinear);
        const line = d3.line().x((_, i) => x(i)).y((d) => y(d)).curve(d3.curveLinear);

        svg.append("path").datum(data).attr("fill", `url(#${id})`).attr("d", area);
        const path = svg.append("path").datum(data)
            .attr("fill", "none").attr("stroke", color)
            .attr("stroke-width", 1.75)
            .attr("stroke-linejoin", "round").attr("stroke-linecap", "round")
            .attr("d", line);

        if (!reduceMotion()) {
            const len = path.node().getTotalLength();
            path.attr("stroke-dasharray", `${len} ${len}`).attr("stroke-dashoffset", len)
                .transition().duration(900).ease(d3.easeCubicOut).attr("stroke-dashoffset", 0);
        }

        // titik denyut di ujung kanan
        const last = data[data.length - 1];
        const g = svg.append("g").attr("transform", `translate(${x(data.length - 1)},${y(last)})`);
        if (!reduceMotion()) {
            g.append("circle").attr("r", 3).attr("fill", color).attr("opacity", 0.45)
                .attr("class", "vd-pulse-ring");
        }
        g.append("circle").attr("r", 2.6).attr("fill", color);
    }

    // ------------------------------------------------------------
    // Area bertumpuk + crosshair
    // ------------------------------------------------------------
    function areaStack(el, series, opts) {
        const p = palette();
        const { w, h } = frame(el);
        const m = { top: 14, right: 14, bottom: 26, left: 46 };
        clear(el);
        if (!series || !series.length || !series[0].points.length) return empty(el, "Belum ada data pada rentang ini");

        const svg = d3.select(el).append("svg").attr("width", w).attr("height", h)
            .attr("role", "img")
            .attr("aria-label", opts.ariaLabel || "Grafik aktivitas");

        const dates = series[0].points.map((d) => new Date(d.date));
        const keys = series.map((s) => s.name);
        const rows = dates.map((dt, i) => {
            const row = { date: dt };
            series.forEach((s) => (row[s.name] = s.points[i] ? s.points[i].value : 0));
            return row;
        });

        // Deret yang saling tumpang tindih (mis. tagihan vs kas masuk) tidak boleh ditumpuk:
        // penjumlahannya tidak punya arti. opts.stack === false menggambar tiap deret dari nol.
        const stack = opts.stack !== false;
        const stacked = stack
            ? d3.stack().keys(keys)(rows)
            : keys.map((k) => Object.assign(rows.map((r) => Object.assign([0, r[k]], { data: r })), { key: k }));

        const x = d3.scaleTime().domain(d3.extent(dates)).range([m.left, w - m.right]);
        const yMax = d3.max(stacked, (l) => d3.max(l, (d) => d[1])) || 1;
        const y = d3.scaleLinear().domain([0, yMax * 1.12]).nice().range([h - m.bottom, m.top]);
        const color = d3.scaleOrdinal().domain(keys).range(p.series);

        svg.append("g").attr("transform", `translate(0,${h - m.bottom})`)
            .call(d3.axisBottom(x).ticks(Math.min(6, dates.length)).tickFormat(fmtDate).tickSizeOuter(0))
            .call((g) => g.select(".domain").attr("stroke", p.grid))
            .call((g) => g.selectAll("text").attr("fill", p.muted).style("font-size", "11px"))
            .call((g) => g.selectAll("line").attr("stroke", p.grid));

        svg.append("g").attr("transform", `translate(${m.left},0)`)
            .call(d3.axisLeft(y).ticks(4).tickFormat(fmtCompact).tickSize(-(w - m.left - m.right)))
            .call((g) => g.select(".domain").remove())
            .call((g) => g.selectAll("text").attr("fill", p.muted).style("font-size", "11px"))
            .call((g) => g.selectAll("line").attr("stroke", p.grid).attr("stroke-dasharray", "2 4"));

        const area = d3.area()
            .x((d) => x(d.data.date)).y0((d) => y(d[0])).y1((d) => y(d[1]))
            .curve(d3.curveMonotoneX);

        svg.append("g").selectAll("path").data(stacked).join("path")
            .attr("fill", (d) => color(d.key))
            .attr("fill-opacity", stack ? 0.82 : 0.36)
            .attr("stroke", (d) => color(d.key))
            .attr("stroke-width", 1)
            .attr("d", area)
            .attr("opacity", reduceMotion() ? 1 : 0)
            .transition().duration(reduceMotion() ? 0 : 500).attr("opacity", 1);

        // crosshair
        const focus = svg.append("g").style("display", "none");
        focus.append("line").attr("y1", m.top).attr("y2", h - m.bottom)
            .attr("stroke", p.muted).attr("stroke-width", 1).attr("stroke-dasharray", "3 3");

        svg.append("rect")
            .attr("x", m.left).attr("y", m.top)
            .attr("width", Math.max(0, w - m.left - m.right)).attr("height", Math.max(0, h - m.top - m.bottom))
            .attr("fill", "transparent")
            .on("mousemove", function (event) {
                const [mx] = d3.pointer(event);
                const dt = x.invert(mx);
                const i = Math.min(rows.length - 1, Math.max(0, d3.bisector((d) => d).left(dates, dt)));
                const row = rows[i];
                focus.style("display", null).select("line").attr("x1", x(row.date)).attr("x2", x(row.date));
                const lines = keys.map((k, ki) =>
                    `<div class="vd-tip-row"><span class="vd-tip-dot" style="background:${p.series[ki % p.series.length]}"></span>${k}<b>${opts.money ? fmtMoney(row[k]) : fmtInt(row[k])}</b></div>`).join("");
                showTip(`<div class="vd-tip-title">${row.date.toLocaleDateString("id-ID", { weekday: "short", day: "2-digit", month: "short" })}</div>${lines}`,
                    event.clientX, event.clientY);
            })
            .on("mouseleave", () => { focus.style("display", "none"); hideTip(); });
    }

    // ------------------------------------------------------------
    // Donut
    // ------------------------------------------------------------
    function donut(el, slices, opts) {
        const p = palette();
        const { w, h } = frame(el);
        clear(el);
        const items = (slices || []).filter((s) => s.value > 0);
        if (!items.length) return empty(el, "Belum ada data");

        const r = Math.min(w, h) / 2 - 6;
        const svg = d3.select(el).append("svg").attr("width", w).attr("height", h)
            .attr("role", "img").attr("aria-label", opts.ariaLabel || "Komposisi");
        const g = svg.append("g").attr("transform", `translate(${w / 2},${h / 2})`);
        const color = d3.scaleOrdinal().domain(items.map((d) => d.label)).range(p.series);
        const total = d3.sum(items, (d) => d.value);

        const arcs = d3.pie().sort(null).value((d) => d.value).padAngle(0.02)(items);
        const arc = d3.arc().innerRadius(r * 0.62).outerRadius(r).cornerRadius(3);
        const arcHover = d3.arc().innerRadius(r * 0.62).outerRadius(r + 5).cornerRadius(3);

        g.selectAll("path").data(arcs).join("path")
            .attr("fill", (d) => color(d.data.label))
            .attr("d", arc)
            .style("cursor", "pointer")
            .on("mousemove", function (event, d) {
                d3.select(this).transition().duration(120).attr("d", arcHover);
                const pct = ((d.data.value / total) * 100).toFixed(1);
                showTip(`<div class="vd-tip-title">${d.data.label}</div><div class="vd-tip-row">${opts.money ? fmtMoney(d.data.value) : fmtInt(d.data.value)}<b>${pct}%</b></div>`,
                    event.clientX, event.clientY);
            })
            .on("mouseleave", function () {
                d3.select(this).transition().duration(120).attr("d", arc);
                hideTip();
            });

        g.append("text").attr("text-anchor", "middle").attr("dy", "-0.1em")
            .attr("fill", p.ink).style("font-size", "1.35rem").style("font-weight", "700")
            .style("font-family", "var(--vd-font-data)")
            .text(opts.money ? fmtCompact(total) : fmtInt(total));
        g.append("text").attr("text-anchor", "middle").attr("dy", "1.35em")
            .attr("fill", p.muted).style("font-size", "0.72rem")
            .style("letter-spacing", "0.08em").style("text-transform", "uppercase")
            .text(opts.centerLabel || "Total");
    }

    // ------------------------------------------------------------
    // Bar horizontal berperingkat
    // ------------------------------------------------------------
    function barsH(el, items, opts) {
        const p = palette();
        const { w } = frame(el);
        clear(el);
        if (!items || !items.length) return empty(el, "Belum ada data");

        const rowH = 34, m = { top: 6, right: 62, bottom: 6, left: 4 };
        const labelW = Math.min(150, Math.max(96, w * 0.34));
        const h = items.length * rowH + m.top + m.bottom;

        const svg = d3.select(el).append("svg").attr("width", w).attr("height", h)
            .attr("role", "img").attr("aria-label", opts.ariaLabel || "Peringkat");
        const x = d3.scaleLinear().domain([0, d3.max(items, (d) => d.value) || 1])
            .range([labelW + 8, w - m.right]);

        const g = svg.selectAll("g.row").data(items).join("g")
            .attr("class", "row")
            .attr("transform", (_, i) => `translate(0,${m.top + i * rowH})`);

        g.append("text").attr("x", 0).attr("y", rowH / 2).attr("dy", "0.1em")
            .attr("fill", p.ink).style("font-size", "0.8rem")
            .text((d) => d.label.length > 22 ? d.label.slice(0, 21) + "…" : d.label)
            .append("title").text((d) => d.label);

        g.append("rect")
            .attr("x", labelW + 8).attr("y", rowH / 2 - 8)
            .attr("height", 16).attr("rx", 5)
            .attr("fill", p.grid).attr("fill-opacity", 0.55)
            .attr("width", Math.max(0, w - m.right - labelW - 8));

        g.append("rect")
            .attr("x", labelW + 8).attr("y", rowH / 2 - 8)
            .attr("height", 16).attr("rx", 5)
            .attr("fill", (_, i) => p.series[i % p.series.length])
            .attr("width", 0)
            .style("cursor", "pointer")
            .on("mousemove", (event, d) => showTip(
                `<div class="vd-tip-title">${d.label}</div>` +
                (d.sub ? `<div class="vd-tip-row">${d.sub}</div>` : "") +
                `<div class="vd-tip-row">${opts.unit || "Jumlah"}<b>${opts.money ? fmtMoney(d.value) : fmtInt(d.value)}</b></div>` +
                (d.secondary ? `<div class="vd-tip-row">${opts.secondaryLabel || "Nilai"}<b>${opts.money ? fmtInt(d.secondary) : fmtMoney(d.secondary)}</b></div>` : ""),
                event.clientX, event.clientY))
            .on("mouseleave", hideTip)
            .transition().duration(reduceMotion() ? 0 : 550).ease(d3.easeCubicOut)
            .attr("width", (d) => Math.max(2, x(d.value) - labelW - 8));

        // angka diletakkan di luar batang, rata kanan, agar tidak menimpa warna batang
        g.append("text")
            .attr("x", w - 4).attr("y", rowH / 2).attr("dy", "0.1em")
            .attr("text-anchor", "end")
            .attr("fill", p.ink).style("font-size", "0.78rem").style("font-weight", "600")
            .style("font-family", "var(--vd-font-data)")
            .text((d) => opts.money ? fmtCompact(d.value) : fmtInt(d.value));
    }

    // ------------------------------------------------------------
    // Heatmap hari x jam
    // ------------------------------------------------------------
    function heatmap(el, cells, opts) {
        const p = palette();
        const { w } = frame(el);
        clear(el);

        const days = ["Min", "Sen", "Sel", "Rab", "Kam", "Jum", "Sab"];
        const hours = d3.range(6, 22);          // jam layanan 06:00-21:00
        const m = { top: 20, right: 8, bottom: 8, left: 34 };
        const cw = Math.max(12, (w - m.left - m.right) / hours.length);
        const ch = 22;
        const h = days.length * ch + m.top + m.bottom;

        const svg = d3.select(el).append("svg").attr("width", w).attr("height", h)
            .attr("role", "img").attr("aria-label", opts.ariaLabel || "Peta kepadatan jam layanan");

        const lookup = new Map((cells || []).map((c) => [`${c.day}-${c.hour}`, c.value]));
        const max = d3.max(cells || [], (c) => c.value) || 1;
        const scale = d3.scaleSequential().domain([0, max])
            .interpolator(d3.interpolate(p.heat[0], p.heat[1]));

        svg.append("g").selectAll("text").data(hours).join("text")
            .attr("x", (_, i) => m.left + i * cw + cw / 2).attr("y", 12)
            .attr("text-anchor", "middle").attr("fill", p.muted)
            .style("font-size", "9px").style("font-family", "var(--vd-font-data)")
            .text((d) => (d % 3 === 0 ? d : ""));

        svg.append("g").selectAll("text").data(days).join("text")
            .attr("x", 0).attr("y", (_, i) => m.top + i * ch + ch / 2 + 3)
            .attr("fill", p.muted).style("font-size", "10px")
            .text((d) => d);

        const g = svg.append("g");
        days.forEach((dayLabel, di) => {
            hours.forEach((hr, hi) => {
                const v = lookup.get(`${di}-${hr}`) || 0;
                g.append("rect")
                    .attr("x", m.left + hi * cw + 1).attr("y", m.top + di * ch + 1)
                    .attr("width", cw - 2).attr("height", ch - 2).attr("rx", 3)
                    .attr("fill", v > 0 ? scale(v) : p.heat[0])
                    .attr("fill-opacity", v > 0 ? 1 : 0.5)
                    .style("cursor", v > 0 ? "pointer" : "default")
                    .on("mousemove", (event) => showTip(
                        `<div class="vd-tip-title">${dayLabel}, ${String(hr).padStart(2, "0")}:00</div>` +
                        `<div class="vd-tip-row">Layanan<b>${fmtInt(v)}</b></div>`,
                        event.clientX, event.clientY))
                    .on("mouseleave", hideTip);
            });
        });
    }

    function empty(el, message) {
        d3.select(el).append("div").attr("class", "vd-chart-empty").text(message);
    }

    // ------------------------------------------------------------
    // API publik untuk Blazor
    // ------------------------------------------------------------
    const renderers = { ecg: ecgSparkline, area: areaStack, donut, bars: barsH, heat: heatmap };

    function draw(id, type, data, opts) {
        const el = document.getElementById(id);
        if (!el || typeof d3 === "undefined") return;
        const fn = renderers[type];
        if (!fn) return;
        try { fn(el, data, opts || {}); }
        catch (e) { console.error("[vdCharts]", type, e); }
    }

    window.vdCharts = {
        render(id, type, data, opts) {
            registry.set(id, { type, data, opts });
            draw(id, type, data, opts);
            observe(id);
        },
        dispose(id) {
            registry.delete(id);
            const el = document.getElementById(id);
            if (el) clear(el);
        },
        redrawAll() {
            registry.forEach((cfg, id) => draw(id, cfg.type, cfg.data, cfg.opts));
        }
    };

    // gambar ulang saat ukuran berubah
    const sizes = new Map();
    let ro = null;
    function observe(id) {
        if (!window.ResizeObserver) return;
        const el = document.getElementById(id);
        if (!el) return;
        if (!ro) {
            ro = new ResizeObserver((entries) => {
                entries.forEach((entry) => {
                    const target = entry.target;
                    const prev = sizes.get(target.id);
                    const nw = Math.round(entry.contentRect.width);
                    if (prev === nw) return;
                    sizes.set(target.id, nw);
                    const cfg = registry.get(target.id);
                    if (cfg) draw(target.id, cfg.type, cfg.data, cfg.opts);
                });
            });
        }
        ro.observe(el);
    }

    // gambar ulang saat tema berganti
    document.addEventListener("vd:themechange", () => window.vdCharts.redrawAll());
    window.addEventListener("beforeprint", () => window.vdCharts.redrawAll());
})();
