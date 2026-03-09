
queueMicrotask(console.log.bind(console, "%c   ___             __\n /\'___\\           /\\ \\\n/\\ \\__/  __  __   \\_\\ \\     __     _ __\n\\ \\ ,__\\/\\ \\/\\ \\  /\'_` \\  /\'__`\\  /\\`\'__\\\n \\ \\ \\_/\\ \\ \\_\\ \\/\\ \\L\\ \\/\\ \\L\\.\\_\\ \\ \\/\n  \\ \\_\\  \\/`____ \\ \\___,_\\ \\__/.\\_\\\\ \\_\\\n   \\/_/   `/___/> \\/__,_ /\\/__/\\/_/ \\/_/\n             /\\___/\n             \\/__/", "font-family: monospace; white-space: nowrap"));
queueMicrotask(console.log.bind(console, "This website has been made using ASP.NET Core and Blazor.\nLike what you see? I\'m available for hire!\n\nhttps://fydar.dev/contact"));

const clamp = (a, min = 0, max = 1) => Math.min(max, Math.max(min, a));
const invlerp = (x, y, a) => (a - x) / (y - x);
function lerp(start, end, amt) {
    return (1 - amt) * start + amt * end
}

function UpdateParallaxLayerSize() {
    var elements = document.getElementsByClassName("parallax-layer");
    for (let i = 0; i < elements.length; i++) {
        var element = elements[i];

        var containerRect = element.parentElement.parentElement.getBoundingClientRect();
        element.style.width = containerRect.width + "px";
        element.style.height = containerRect.height + "px";
    }
}
function UpdateRelativeElements() {
    var elements = document.getElementsByClassName("parallax-focalanchortop");
    for (let i = 0; i < elements.length; i++) {
        var element = elements[i];
        var clientRect = element.getBoundingClientRect()
        var time = clamp(invlerp(clientRect.height, -clientRect.height, clientRect.top), 0, 1);
        element.style.setProperty("--animation-time", time.toFixed(5));
        element.style.setProperty("--parallax-offset", lerp(-clientRect.height, clientRect.height, time).toFixed(1));
    }
}

UpdateParallaxLayerSize();
UpdateRelativeElements();

window.addEventListener("scroll",
    eventArgs => {
        UpdateRelativeElements();
    }, { passive: true }
);

window.addEventListener("resize",
    eventArgs => {
        UpdateParallaxLayerSize();
        UpdateRelativeElements();
    }, { passive: true }
);



function createStylesheet() {
    const style = document.createElement('style');
    document.head.appendChild(style);
    return style.sheet;
}

function addCSSRule(stylesheet, selector, styles) {
    if (stylesheet.insertRule) { // Modern browsers
        stylesheet.insertRule(`${selector} { ${styles} }`, stylesheet.cssRules.length);
    } else if (stylesheet.addRule) { // Older IE (rarely needed now)
        stylesheet.addRule(selector, styles, -1);
    } else {
        console.error("Adding CSS rule not supported.");
    }
}

function findCSSRule(stylesheet, selector) {
    const rules = stylesheet.cssRules || stylesheet.rules;
    for (let i = 0; i < rules.length; i++) {
        const rule = rules[i];
        if (rule instanceof CSSStyleRule && rule.selectorText === selector) {
            return rule;
        }
    }
    return null;
}

var proceduralStylesheet = createStylesheet();
addCSSRule(proceduralStylesheet, ".card", "--pointer-fixed: 0px 0px;");
const rule = findCSSRule(proceduralStylesheet, ".card");

var lastX = 0;
var lastY = 0;

window.addEventListener("pointermove",
    eventArgs => {
        lastX = eventArgs.clientX;
        lastY = eventArgs.clientY;

        rule.style.setProperty("--pointer-fixed", lastX.toFixed(2) + "px " + lastY.toFixed(2) + "px");
    }, { passive: true }
);

window.document.addEventListener("pointerleave",
    eventArgs => {
        var pointerRelativeElements = document.getElementsByClassName("card");

        for (let i = 0; i < pointerRelativeElements.length; i++) {
            var pointerRelativeElement = pointerRelativeElements[i];
            pointerRelativeElement.classList.add("pointer-none");
        }
    }, { passive: true }
);

window.document.addEventListener("pointerenter",
    eventArgs => {
        var pointerRelativeElements = document.getElementsByClassName("card");

        for (let i = 0; i < pointerRelativeElements.length; i++) {
            var pointerRelativeElement = pointerRelativeElements[i];
            pointerRelativeElement.classList.remove("pointer-none");
        }
    }, { passive: true }
);



// Cache section positions to avoid layout thrashing during scroll
let sectionCache = [];

function updateSectionCache() {
    sectionCache = Array.from(document.querySelectorAll("menu.toc li > a")).map(link => {
        const id = link.getAttribute("href").split('#').pop();
        const section = document.getElementById(id);
        if (!section) return null;

        // We only read these when the window resizes or loads
        const rect = section.getBoundingClientRect();
        const top = rect.top + window.scrollY;
        return {
            link,
            top: top,
            bottom: top + section.offsetHeight,
            center: top + (section.offsetHeight / 2)
        };
    }).filter(Boolean);
}

// Initialize cache and update on resize
window.addEventListener('resize', updateSectionCache);
updateSectionCache();

function NavHighlighter() {
    const menus = document.querySelectorAll("menu.toc");
    const scrollY = window.scrollY;
    const viewportHeight = window.innerHeight;
    const viewportCenter = scrollY + (viewportHeight * 0.333);

    menus.forEach(menu => {
        // 1. Guard: Check if display: none
        // offsetParent is null if the element or any ancestor is display: none
        if (menu.offsetParent === null) return;

        // 2. Guard: Check if menu is visible on screen (scrolled past)
        const menuRect = menu.getBoundingClientRect();
        const isMenuVisible = menuRect.top < viewportHeight && menuRect.bottom > 0;
        if (!isMenuVisible) return;

        let closestLink = null;
        let minDistance = Infinity;

        // 3. Use cached values (No Recalculate Style calls here!)
        sectionCache.forEach((data) => {
            const distance = Math.abs(viewportCenter - data.center);
            if (distance < minDistance) {
                minDistance = distance;
                closestLink = data.link;
            }
        });

        const firstTop = sectionCache[0]?.top ?? 0;
        const lastBottom = sectionCache[sectionCache.length - 1]?.bottom ?? 0;

        const isBeforeFirst = scrollY < (firstTop - (viewportHeight * 0.333));
        const isAfterLast = (scrollY + viewportHeight) > (lastBottom + (viewportHeight * 0.333));
        const isOutOfBounds = isBeforeFirst || isAfterLast;

        // 4. Batch the Writes (DOM updates) at the end
        const links = menu.querySelectorAll("li > a");
        links.forEach(link => {
            const shouldBeActive = !isOutOfBounds && link === closestLink;
            // Only update DOM if state actually changed to minimize paint
            if (link.classList.contains("active") !== shouldBeActive) {
                link.classList.toggle("active", shouldBeActive);
            }
        });
    });
}

NavHighlighter();

// window.addEventListener("scroll",
//     eventArgs => {
//         NavHighlighter();
//     }, { passive: true }
// );

let scrollTick = false;
window.addEventListener("scroll", () => {
    if (!scrollTick) {
        window.requestAnimationFrame(() => {
            NavHighlighter();
            scrollTick = false;
        });
        scrollTick = true;
    }
}, { passive: true });

// let throttleTimeout = null;
// let lastRun = 0;
// function throttledNavHighlighter() {
//     const now = Date.now();
//     const limit = 100; // 0.1 seconds
// 
//     if (now - lastRun < limit) {
//         // If we scroll again within 50ms, clear the previous "eventual" 
//         // run and schedule a new one.
//         clearTimeout(throttleTimeout);
//         throttleTimeout = setTimeout(() => {
//             lastRun = now;
//             NavHighlighter();
//         }, limit);
//     } else {
//         // If it's been longer than 50ms, run immediately
//         lastRun = now;
//         NavHighlighter();
//     }
// }
// window.addEventListener("scroll", throttledNavHighlighter, { passive: true });

window.addEventListener("resize",
    eventArgs => {
        NavHighlighter();
    }, { passive: true }
);



function GraphScaler() {
    var elements = document.getElementsByClassName("graph");
    for (let i = 0; i < elements.length; i++) {
        var element = elements[i];
        var clientRect = element.getBoundingClientRect()

        var targetSize = window.getComputedStyle(element).getPropertyValue("--Lodestone-graph-targetsize");
        element.style.setProperty("--Lodestone-graph-scale", clientRect.width / targetSize);
    }
}

GraphScaler();

window.addEventListener("resize",
    eventArgs => {
        GraphScaler();
    }, { passive: true }
);


const getRequiredModifiers = () => {
    const userAgent = navigator.userAgent.toLowerCase();
    const isMac = navigator.platform.toLowerCase().includes('mac');

    // Check for Firefox: Modern browsers use userAgentData, 
    // but Firefox doesn't support it yet, so we check the string.
    const isFirefox = userAgent.includes('firefox') || userAgent.includes('fxios');

    return {
        requiresCtrl: isMac,
        requiresAlt: true,
        // Firefox on Windows/Linux requires Shift. On Mac, it uses Ctrl+Opt.
        requiresShift: isFirefox && !isMac
    };
};

document.addEventListener('DOMContentLoaded', () => {
    // Is Access Keys supported by the browser? If so, use that implemention for Hyperlink activation.
    if (!('accessKey' in document.createElement('a'))) return;

    const config = getRequiredModifiers();

    // Initialize state from sessionStorage (or default to false)
    const savedState = JSON.parse(sessionStorage.getItem('accessKeyState')) || {};
    let keysPressed = {
        Alt: savedState.Alt || false,
        Control: false,
        Shift: savedState.Shift || false
    };

    const altCondition = keysPressed.Alt;
    const ctrlCondition = config.requiresCtrl ? keysPressed.Control : true;
    const shiftCondition = config.requiresShift ? keysPressed.Shift : true;

    const shouldShow = altCondition && ctrlCondition && shiftCondition;

    document.querySelectorAll('a + .access-hint').forEach(hint => {
        if (shouldShow) {
            hint.classList.toggle('show', true);
            hint.classList.toggle('no-transition', true);
        }
    });

    const updateHintVisibility = () => {
        const altCondition = keysPressed.Alt;
        const ctrlCondition = config.requiresCtrl ? keysPressed.Control : true;
        const shiftCondition = config.requiresShift ? keysPressed.Shift : true;

        const shouldShow = altCondition && ctrlCondition && shiftCondition;

        document.querySelectorAll('a + .access-hint').forEach(hint => {
            hint.classList.toggle('show', shouldShow);
            hint.classList.toggle('no-transition', false);
        });

        // Save state for the next page load.
        sessionStorage.setItem('accessKeyState', JSON.stringify({
            Alt: keysPressed.Alt,
            Shift: keysPressed.Shift
        }));
    };

    const syncKeys = (e) => {
        if (!e || typeof e.getModifierState !== 'function') return;

        keysPressed.Alt = e.getModifierState("Alt");
        keysPressed.Control = e.getModifierState("Control");
        keysPressed.Shift = e.getModifierState("Shift");

        updateHintVisibility();
    };

    // High-frequency syncs (only once to catch initial state)
    window.addEventListener('mousemove', syncKeys, { once: true });
    window.addEventListener('pointermove', syncKeys, { once: true });

    // Interaction syncs
    window.addEventListener('mousedown', syncKeys);
    window.addEventListener('keydown', syncKeys);
    window.addEventListener('keyup', syncKeys);
    window.addEventListener('contextmenu', syncKeys);

    // Navigation/Tab syncs
    window.addEventListener('focus', syncKeys);
    window.addEventListener('pageshow', syncKeys);

    // Reset on blur to prevent "stuck" keys
    window.addEventListener('blur', () => {
        keysPressed = { Alt: false, Control: false, Shift: false };
        updateHintVisibility();
    });
});
