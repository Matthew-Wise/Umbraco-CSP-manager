(function(){function r(e,n,t){function o(i,f){if(!n[i]){if(!e[i]){var c="function"==typeof require&&require;if(!f&&c)return c(i,!0);if(u)return u(i,!0);var a=new Error("Cannot find module '"+i+"'");throw a.code="MODULE_NOT_FOUND",a}var p=n[i]={exports:{}};e[i][0].call(p.exports,function(r){var n=e[i][1][r];return o(n||r)},p,p.exports,r,e,n,t)}return n[i].exports}for(var u="function"==typeof require&&require,i=0;i<t.length;i++)o(t[i]);return o}return r})()({1:[function(require,module,exports){
"use strict";

var _evaluator = require("csp_evaluator/dist/evaluator.js");
var _parser = require("csp_evaluator/dist/parser.js");
window.CspEvaluator = _evaluator.CspEvaluator;
window.CspParser = _parser.CspParser;

},{"csp_evaluator/dist/evaluator.js":9,"csp_evaluator/dist/parser.js":11}],2:[function(require,module,exports){
"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.URLS = void 0;
exports.URLS = [
    '//gstatic.com/fsn/angular_js-bundle1.js',
    '//www.gstatic.com/fsn/angular_js-bundle1.js',
    '//www.googleadservices.com/pageadimg/imgad',
    '//yandex.st/angularjs/1.2.16/angular-cookies.min.js',
    '//yastatic.net/angularjs/1.2.23/angular.min.js',
    '//yuedust.yuedu.126.net/js/components/angular/angular.js',
    '//art.jobs.netease.com/script/angular.js',
    '//csu-c45.kxcdn.com/angular/angular.js',
    '//elysiumwebsite.s3.amazonaws.com/uploads/blog-media/rockstar/angular.min.js',
    '//inno.blob.core.windows.net/new/libs/AngularJS/1.2.1/angular.min.js',
    '//gift-talk.kakao.com/public/javascripts/angular.min.js',
    '//ajax.googleapis.com/ajax/libs/angularjs/1.2.0rc1/angular-route.min.js',
    '//master-sumok.ru/vendors/angular/angular-cookies.js',
    '//ayicommon-a.akamaihd.net/static/vendor/angular-1.4.2.min.js',
    '//pangxiehaitao.com/framework/angular-1.3.9/angular-animate.min.js',
    '//cdnjs.cloudflare.com/ajax/libs/angular.js/1.2.16/angular.min.js',
    '//96fe3ee995e96e922b6b-d10c35bd0a0de2c718b252bc575fdb73.ssl.cf1.rackcdn.com/angular.js',
    '//oss.maxcdn.com/angularjs/1.2.20/angular.min.js',
    '//reports.zemanta.com/smedia/common/angularjs/1.2.11/angular.js',
    '//cdn.shopify.com/s/files/1/0225/6463/t/1/assets/angular-animate.min.js',
    '//parademanagement.com.s3-website-ap-southeast-1.amazonaws.com/js/angular.min.js',
    '//cdn.jsdelivr.net/angularjs/1.1.2/angular.min.js',
    '//eb2883ede55c53e09fd5-9c145fb03d93709ea57875d307e2d82e.ssl.cf3.rackcdn.com/components/angular-resource.min.js',
    '//andors-trail.googlecode.com/git/AndorsTrailEdit/lib/angular.min.js',
    '//cdn.walkme.com/General/EnvironmentTests/angular/angular.min.js',
    '//laundrymail.com/angular/angular.js',
    '//s3-eu-west-1.amazonaws.com/staticancpa/js/angular-cookies.min.js',
    '//collade.demo.stswp.com/js/vendor/angular.min.js',
    '//mrfishie.github.io/sailor/bower_components/angular/angular.min.js',
    '//askgithub.com/static/js/angular.min.js',
    '//services.amazon.com/solution-providers/assets/vendor/angular-cookies.min.js',
    '//raw.githubusercontent.com/angular/code.angularjs.org/master/1.0.7/angular-resource.js',
    '//prb-resume.appspot.com/bower_components/angular-animate/angular-animate.js',
    '//dl.dropboxusercontent.com/u/30877786/angular.min.js',
    '//static.tumblr.com/x5qdx0r/nPOnngtff/angular-resource.min_1_.js',
    '//storage.googleapis.com/assets-prod.urbansitter.net/us-sym/assets/vendor/angular-sanitize/angular-sanitize.min.js',
    '//twitter.github.io/labella.js/bower_components/angular/angular.min.js',
    '//cdn2-casinoroom.global.ssl.fastly.net/js/lib/angular-animate.min.js',
    '//www.adobe.com/devnet-apps/flashshowcase/lib/angular/angular.1.1.5.min.js',
    '//eternal-sunset.herokuapp.com/bower_components/angular/angular.js',
    '//cdn.bootcss.com/angular.js/1.2.0/angular.min.js'
];

},{}],3:[function(require,module,exports){
"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.URLS = void 0;
exports.URLS = [
    '//vk.com/swf/video.swf',
    '//ajax.googleapis.com/ajax/libs/yui/2.8.0r4/build/charts/assets/charts.swf'
];

},{}],4:[function(require,module,exports){
"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.URLS = exports.NEEDS_EVAL = void 0;
exports.NEEDS_EVAL = [
    'googletagmanager.com', 'www.googletagmanager.com',
    'www.googleadservices.com', 'google-analytics.com',
    'ssl.google-analytics.com', 'www.google-analytics.com'
];
exports.URLS = [
    '//bebezoo.1688.com/fragment/index.htm',
    '//www.google-analytics.com/gtm/js',
    '//googleads.g.doubleclick.net/pagead/conversion/1036918760/wcm',
    '//www.googleadservices.com/pagead/conversion/1070110417/wcm',
    '//www.google.com/tools/feedback/escalation-options',
    '//pin.aliyun.com/check_audio',
    '//offer.alibaba.com/market/CID100002954/5/fetchKeyword.do',
    '//ccrprod.alipay.com/ccr/arriveTime.json',
    '//group.aliexpress.com/ajaxAcquireGroupbuyProduct.do',
    '//detector.alicdn.com/2.7.3/index.php',
    '//suggest.taobao.com/sug',
    '//translate.google.com/translate_a/l',
    '//count.tbcdn.cn//counter3',
    '//wb.amap.com/channel.php',
    '//translate.googleapis.com/translate_a/l',
    '//afpeng.alimama.com/ex',
    '//accounts.google.com/o/oauth2/revoke',
    '//pagead2.googlesyndication.com/relatedsearch',
    '//yandex.ru/soft/browsers/check',
    '//api.facebook.com/restserver.php',
    '//mts0.googleapis.com/maps/vt',
    '//syndication.twitter.com/widgets/timelines/765840589183213568',
    '//www.youtube.com/profile_style',
    '//googletagmanager.com/gtm/js',
    '//mc.yandex.ru/watch/24306916/1',
    '//share.yandex.net/counter/gpp/',
    '//ok.go.mail.ru/lady_on_lady_recipes_r.json',
    '//d1f69o4buvlrj5.cloudfront.net/__efa_15_1_ornpba.xekq.arg/optout_check',
    '//www.googletagmanager.com/gtm/js',
    '//api.vk.com/method/wall.get',
    '//www.sharethis.com/get-publisher-info.php',
    '//google.ru/maps/vt',
    '//pro.netrox.sc/oapi/h_checksite.ashx',
    '//vimeo.com/api/oembed.json/',
    '//de.blog.newrelic.com/wp-admin/admin-ajax.php',
    '//ajax.googleapis.com/ajax/services/search/news',
    '//ssl.google-analytics.com/gtm/js',
    '//pubsub.pubnub.com/subscribe/demo/hello_world/',
    '//pass.yandex.ua/services',
    '//id.rambler.ru/script/topline_info.js',
    '//m.addthis.com/live/red_lojson/100eng.json',
    '//passport.ngs.ru/ajax/check',
    '//catalog.api.2gis.ru/ads/search',
    '//gum.criteo.com/sync',
    '//maps.google.com/maps/vt',
    '//ynuf.alipay.com/service/um.json',
    '//securepubads.g.doubleclick.net/gampad/ads',
    '//c.tiles.mapbox.com/v3/texastribune.tx-congress-cvap/6/15/26.grid.json',
    '//rexchange.begun.ru/banners',
    '//an.yandex.ru/page/147484',
    '//links.services.disqus.com/api/ping',
    '//api.map.baidu.com/',
    '//tj.gongchang.com/api/keywordrecomm/',
    '//data.gongchang.com/livegrail/',
    '//ulogin.ru/token.php',
    '//beta.gismeteo.ru/api/informer/layout.js/120x240-3/ru/',
    '//maps.googleapis.com/maps/api/js/GeoPhotoService.GetMetadata',
    '//a.config.skype.com/config/v1/Skype/908_1.33.0.111/SkypePersonalization',
    '//maps.beeline.ru/w',
    '//target.ukr.net/',
    '//www.meteoprog.ua/data/weather/informer/Poltava.js',
    '//cdn.syndication.twimg.com/widgets/timelines/599200054310604802',
    '//wslocker.ru/client/user.chk.php',
    '//community.adobe.com/CommunityPod/getJSON',
    '//maps.google.lv/maps/vt',
    '//dev.virtualearth.net/REST/V1/Imagery/Metadata/AerialWithLabels/26.318581',
    '//awaps.yandex.ru/10/8938/02400400.',
    '//a248.e.akamai.net/h5.hulu.com/h5.mp4',
    '//nominatim.openstreetmap.org/',
    '//plugins.mozilla.org/en-us/plugins_list.json',
    '//h.cackle.me/widget/32153/bootstrap',
    '//graph.facebook.com/1/',
    '//fellowes.ugc.bazaarvoice.com/data/reviews.json',
    '//widgets.pinterest.com/v3/pidgets/boards/ciciwin/hedgehog-squirrel-crafts/pins/',
    '//se.wikipedia.org/w/api.php',
    '//cse.google.com/api/007627024705277327428/cse/r3vs7b0fcli/queries/js',
    '//relap.io/api/v2/similar_pages_jsonp.js',
    '//c1n3.hypercomments.com/stream/subscribe',
    '//maps.google.de/maps/vt',
    '//books.google.com/books',
    '//connect.mail.ru/share_count',
    '//tr.indeed.com/m/newjobs',
    '//www-onepick-opensocial.googleusercontent.com/gadgets/proxy',
    '//www.panoramio.com/map/get_panoramas.php',
    '//client.siteheart.com/streamcli/client',
    '//www.facebook.com/restserver.php',
    '//autocomplete.travelpayouts.com/avia',
    '//www.googleapis.com/freebase/v1/topic/m/0344_',
    '//mts1.googleapis.com/mapslt/ft',
    '//publish.twitter.com/oembed',
    '//fast.wistia.com/embed/medias/o75jtw7654.json',
    '//partner.googleadservices.com/gampad/ads',
    '//pass.yandex.ru/services',
    '//gupiao.baidu.com/stocks/stockbets',
    '//widget.admitad.com/widget/init',
    '//api.instagram.com/v1/tags/partykungen23328/media/recent',
    '//video.media.yql.yahoo.com/v1/video/sapi/streams/063fb76c-6c70-38c5-9bbc-04b7c384de2b',
    '//ib.adnxs.com/jpt',
    '//pass.yandex.com/services',
    '//www.google.de/maps/vt',
    '//clients1.google.com/complete/search',
    '//api.userlike.com/api/chat/slot/proactive/',
    '//www.youku.com/index_cookielist/s/jsonp',
    '//mt1.googleapis.com/mapslt/ft',
    '//api.mixpanel.com/track/',
    '//wpd.b.qq.com/cgi/get_sign.php',
    '//pipes.yahooapis.com/pipes/pipe.run',
    '//gdata.youtube.com/feeds/api/videos/WsJIHN1kNWc',
    '//9.chart.apis.google.com/chart',
    '//cdn.syndication.twitter.com/moments/709229296800440320',
    '//api.flickr.com/services/feeds/photos_friends.gne',
    '//cbks0.googleapis.com/cbk',
    '//www.blogger.com/feeds/5578653387562324002/posts/summary/4427562025302749269',
    '//query.yahooapis.com/v1/public/yql',
    '//kecngantang.blogspot.com/feeds/posts/default/-/Komik',
    '//www.travelpayouts.com/widgets/50f53ce9ada1b54bcc000031.json',
    '//i.cackle.me/widget/32586/bootstrap',
    '//translate.yandex.net/api/v1.5/tr.json/detect',
    '//a.tiles.mapbox.com/v3/zentralmedia.map-n2raeauc.jsonp',
    '//maps.google.ru/maps/vt',
    '//c1n2.hypercomments.com/stream/subscribe',
    '//rec.ydf.yandex.ru/cookie',
    '//cdn.jsdelivr.net'
];

},{}],5:[function(require,module,exports){
"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    Object.defineProperty(o, k2, { enumerable: true, get: function() { return m[k]; } });
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.checkInvalidKeyword = exports.checkMissingSemicolon = exports.checkUnknownDirective = void 0;
const csp = __importStar(require("../csp"));
const csp_1 = require("../csp");
const finding_1 = require("../finding");
function checkUnknownDirective(parsedCsp) {
    const findings = [];
    for (const directive of Object.keys(parsedCsp.directives)) {
        if (csp.isDirective(directive)) {
            continue;
        }
        if (directive.endsWith(':')) {
            findings.push(new finding_1.Finding(finding_1.Type.UNKNOWN_DIRECTIVE, 'CSP directives don\'t end with a colon.', finding_1.Severity.SYNTAX, directive));
        }
        else {
            findings.push(new finding_1.Finding(finding_1.Type.UNKNOWN_DIRECTIVE, 'Directive "' + directive + '" is not a known CSP directive.', finding_1.Severity.SYNTAX, directive));
        }
    }
    return findings;
}
exports.checkUnknownDirective = checkUnknownDirective;
function checkMissingSemicolon(parsedCsp) {
    const findings = [];
    for (const [directive, directiveValues] of Object.entries(parsedCsp.directives)) {
        if (directiveValues === undefined) {
            continue;
        }
        for (const value of directiveValues) {
            if (csp.isDirective(value)) {
                findings.push(new finding_1.Finding(finding_1.Type.MISSING_SEMICOLON, 'Did you forget the semicolon? ' +
                    '"' + value + '" seems to be a directive, not a value.', finding_1.Severity.SYNTAX, directive, value));
            }
        }
    }
    return findings;
}
exports.checkMissingSemicolon = checkMissingSemicolon;
function checkInvalidKeyword(parsedCsp) {
    const findings = [];
    const keywordsNoTicks = Object.values(csp_1.Keyword).map((k) => k.replace(/'/g, ''));
    for (const [directive, directiveValues] of Object.entries(parsedCsp.directives)) {
        if (directiveValues === undefined) {
            continue;
        }
        for (const value of directiveValues) {
            if (keywordsNoTicks.some((k) => k === value) ||
                value.startsWith('nonce-') ||
                value.match(/^(sha256|sha384|sha512)-/)) {
                findings.push(new finding_1.Finding(finding_1.Type.INVALID_KEYWORD, 'Did you forget to surround "' + value + '" with single-ticks?', finding_1.Severity.SYNTAX, directive, value));
                continue;
            }
            if (!value.startsWith('\'')) {
                continue;
            }
            if (directive === csp.Directive.REQUIRE_TRUSTED_TYPES_FOR) {
                if (value === csp.TrustedTypesSink.SCRIPT) {
                    continue;
                }
            }
            else if (directive === csp.Directive.TRUSTED_TYPES) {
                if (value === '\'allow-duplicates\'' || value === '\'none\'') {
                    continue;
                }
            }
            else {
                if (csp.isKeyword(value) || csp.isHash(value) || csp.isNonce(value)) {
                    continue;
                }
            }
            findings.push(new finding_1.Finding(finding_1.Type.INVALID_KEYWORD, value + ' seems to be an invalid CSP keyword.', finding_1.Severity.SYNTAX, directive, value));
        }
    }
    return findings;
}
exports.checkInvalidKeyword = checkInvalidKeyword;

},{"../csp":8,"../finding":10}],6:[function(require,module,exports){
"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    Object.defineProperty(o, k2, { enumerable: true, get: function() { return m[k]; } });
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.checkHasConfiguredReporting = exports.checkSrcHttp = exports.checkNonceLength = exports.checkDeprecatedDirective = exports.checkIpSource = exports.looksLikeIpAddress = exports.checkFlashObjectAllowlistBypass = exports.checkScriptAllowlistBypass = exports.checkMissingDirectives = exports.checkMultipleMissingBaseUriDirective = exports.checkMissingBaseUriDirective = exports.checkMissingScriptSrcDirective = exports.checkMissingObjectSrcDirective = exports.checkWildcards = exports.checkPlainUrlSchemes = exports.checkScriptUnsafeEval = exports.checkScriptUnsafeInline = exports.URL_SCHEMES_CAUSING_XSS = exports.DIRECTIVES_CAUSING_XSS = void 0;
const angular = __importStar(require("../allowlist_bypasses/angular"));
const flash = __importStar(require("../allowlist_bypasses/flash"));
const jsonp = __importStar(require("../allowlist_bypasses/jsonp"));
const csp = __importStar(require("../csp"));
const csp_1 = require("../csp");
const finding_1 = require("../finding");
const utils = __importStar(require("../utils"));
exports.DIRECTIVES_CAUSING_XSS = [
    csp_1.Directive.SCRIPT_SRC, csp_1.Directive.SCRIPT_SRC_ATTR, csp_1.Directive.SCRIPT_SRC_ELEM,
    csp_1.Directive.OBJECT_SRC, csp_1.Directive.BASE_URI
];
exports.URL_SCHEMES_CAUSING_XSS = ['data:', 'http:', 'https:'];
function checkScriptUnsafeInline(effectiveCsp) {
    const violations = [];
    const directivesToCheck = effectiveCsp.getEffectiveDirectives([
        csp_1.Directive.SCRIPT_SRC, csp_1.Directive.SCRIPT_SRC_ATTR, csp_1.Directive.SCRIPT_SRC_ELEM
    ]);
    for (const directive of directivesToCheck) {
        const values = effectiveCsp.directives[directive] || [];
        if (values.includes(csp_1.Keyword.UNSAFE_INLINE)) {
            violations.push(new finding_1.Finding(finding_1.Type.SCRIPT_UNSAFE_INLINE, `'unsafe-inline' allows the execution of unsafe in-page scripts ` +
                'and event handlers.', finding_1.Severity.HIGH, directive, csp_1.Keyword.UNSAFE_INLINE));
        }
        if (values.includes(csp_1.Keyword.UNSAFE_HASHES)) {
            violations.push(new finding_1.Finding(finding_1.Type.SCRIPT_UNSAFE_HASHES, `'unsafe-hashes', while safer than 'unsafe-inline', allows the execution of unsafe in-page scripts and event handlers as long as their hashes appear in the CSP. Please refactor them to no longer use inline scripts if possible.`, finding_1.Severity.MEDIUM_MAYBE, directive, csp_1.Keyword.UNSAFE_HASHES));
        }
    }
    return violations;
}
exports.checkScriptUnsafeInline = checkScriptUnsafeInline;
function checkScriptUnsafeEval(parsedCsp) {
    const violations = [];
    const directivesToCheck = parsedCsp.getEffectiveDirectives([
        csp_1.Directive.SCRIPT_SRC, csp_1.Directive.SCRIPT_SRC_ATTR, csp_1.Directive.SCRIPT_SRC_ELEM
    ]);
    for (const directive of directivesToCheck) {
        const values = parsedCsp.directives[directive] || [];
        if (values.includes(csp_1.Keyword.UNSAFE_EVAL)) {
            violations.push(new finding_1.Finding(finding_1.Type.SCRIPT_UNSAFE_EVAL, `'unsafe-eval' allows the execution of code injected into DOM APIs ` +
                'such as eval().', finding_1.Severity.MEDIUM_MAYBE, directive, csp_1.Keyword.UNSAFE_EVAL));
        }
    }
    return violations;
}
exports.checkScriptUnsafeEval = checkScriptUnsafeEval;
function checkPlainUrlSchemes(parsedCsp) {
    const violations = [];
    const directivesToCheck = parsedCsp.getEffectiveDirectives(exports.DIRECTIVES_CAUSING_XSS);
    for (const directive of directivesToCheck) {
        const values = parsedCsp.directives[directive] || [];
        for (const value of values) {
            if (exports.URL_SCHEMES_CAUSING_XSS.includes(value)) {
                violations.push(new finding_1.Finding(finding_1.Type.PLAIN_URL_SCHEMES, value + ' URI in ' + directive + ' allows the execution of ' +
                    'unsafe scripts.', finding_1.Severity.HIGH, directive, value));
            }
        }
    }
    return violations;
}
exports.checkPlainUrlSchemes = checkPlainUrlSchemes;
function checkWildcards(parsedCsp) {
    const violations = [];
    const directivesToCheck = parsedCsp.getEffectiveDirectives(exports.DIRECTIVES_CAUSING_XSS);
    for (const directive of directivesToCheck) {
        const values = parsedCsp.directives[directive] || [];
        for (const value of values) {
            const url = utils.getSchemeFreeUrl(value);
            if (url === '*') {
                violations.push(new finding_1.Finding(finding_1.Type.PLAIN_WILDCARD, directive + ` should not allow '*' as source`, finding_1.Severity.HIGH, directive, value));
                continue;
            }
        }
    }
    return violations;
}
exports.checkWildcards = checkWildcards;
function checkMissingObjectSrcDirective(parsedCsp) {
    let objectRestrictions = [];
    if (csp_1.Directive.OBJECT_SRC in parsedCsp.directives) {
        objectRestrictions = parsedCsp.directives[csp_1.Directive.OBJECT_SRC];
    }
    else if (csp_1.Directive.DEFAULT_SRC in parsedCsp.directives) {
        objectRestrictions = parsedCsp.directives[csp_1.Directive.DEFAULT_SRC];
    }
    if (objectRestrictions !== undefined && objectRestrictions.length >= 1) {
        return [];
    }
    return [new finding_1.Finding(finding_1.Type.MISSING_DIRECTIVES, `Missing object-src allows the injection of plugins which can execute JavaScript. Can you set it to 'none'?`, finding_1.Severity.HIGH, csp_1.Directive.OBJECT_SRC)];
}
exports.checkMissingObjectSrcDirective = checkMissingObjectSrcDirective;
function checkMissingScriptSrcDirective(parsedCsp) {
    if (csp_1.Directive.SCRIPT_SRC in parsedCsp.directives ||
        csp_1.Directive.DEFAULT_SRC in parsedCsp.directives) {
        return [];
    }
    return [new finding_1.Finding(finding_1.Type.MISSING_DIRECTIVES, 'script-src directive is missing.', finding_1.Severity.HIGH, csp_1.Directive.SCRIPT_SRC)];
}
exports.checkMissingScriptSrcDirective = checkMissingScriptSrcDirective;
function checkMissingBaseUriDirective(parsedCsp) {
    return checkMultipleMissingBaseUriDirective([parsedCsp]);
}
exports.checkMissingBaseUriDirective = checkMissingBaseUriDirective;
function checkMultipleMissingBaseUriDirective(parsedCsps) {
    const needsBaseUri = (csp) => (csp.policyHasScriptNonces() ||
        (csp.policyHasScriptHashes() && csp.policyHasStrictDynamic()));
    const hasBaseUri = (csp) => csp_1.Directive.BASE_URI in csp.directives;
    if (parsedCsps.some(needsBaseUri) && !parsedCsps.some(hasBaseUri)) {
        const description = 'Missing base-uri allows the injection of base tags. ' +
            'They can be used to set the base URL for all relative (script) ' +
            'URLs to an attacker controlled domain. ' +
            `Can you set it to 'none' or 'self'?`;
        return [new finding_1.Finding(finding_1.Type.MISSING_DIRECTIVES, description, finding_1.Severity.HIGH, csp_1.Directive.BASE_URI)];
    }
    return [];
}
exports.checkMultipleMissingBaseUriDirective = checkMultipleMissingBaseUriDirective;
function checkMissingDirectives(parsedCsp) {
    return [
        ...checkMissingObjectSrcDirective(parsedCsp),
        ...checkMissingScriptSrcDirective(parsedCsp),
        ...checkMissingBaseUriDirective(parsedCsp),
    ];
}
exports.checkMissingDirectives = checkMissingDirectives;
function checkScriptAllowlistBypass(parsedCsp) {
    const violations = [];
    parsedCsp
        .getEffectiveDirectives([csp_1.Directive.SCRIPT_SRC, csp_1.Directive.SCRIPT_SRC_ELEM])
        .forEach(effectiveScriptSrcDirective => {
        const scriptSrcValues = parsedCsp.directives[effectiveScriptSrcDirective] || [];
        if (scriptSrcValues.includes(csp_1.Keyword.NONE)) {
            return;
        }
        for (const value of scriptSrcValues) {
            if (value === csp_1.Keyword.SELF) {
                violations.push(new finding_1.Finding(finding_1.Type.SCRIPT_ALLOWLIST_BYPASS, `'self' can be problematic if you host JSONP, AngularJS or user ` +
                    'uploaded files.', finding_1.Severity.MEDIUM_MAYBE, effectiveScriptSrcDirective, value));
                continue;
            }
            if (value.startsWith('\'')) {
                continue;
            }
            if (csp.isUrlScheme(value) || value.indexOf('.') === -1) {
                continue;
            }
            const url = '//' + utils.getSchemeFreeUrl(value);
            const angularBypass = utils.matchWildcardUrls(url, angular.URLS);
            let jsonpBypass = utils.matchWildcardUrls(url, jsonp.URLS);
            if (jsonpBypass) {
                const evalRequired = jsonp.NEEDS_EVAL.includes(jsonpBypass.hostname);
                const evalPresent = scriptSrcValues.includes(csp_1.Keyword.UNSAFE_EVAL);
                if (evalRequired && !evalPresent) {
                    jsonpBypass = null;
                }
            }
            if (jsonpBypass || angularBypass) {
                let bypassDomain = '';
                let bypassTxt = '';
                if (jsonpBypass) {
                    bypassDomain = jsonpBypass.hostname;
                    bypassTxt = ' JSONP endpoints';
                }
                if (angularBypass) {
                    bypassDomain = angularBypass.hostname;
                    bypassTxt += (bypassTxt.trim() === '') ? '' : ' and';
                    bypassTxt += ' Angular libraries';
                }
                violations.push(new finding_1.Finding(finding_1.Type.SCRIPT_ALLOWLIST_BYPASS, bypassDomain + ' is known to host' + bypassTxt +
                    ' which allow to bypass this CSP.', finding_1.Severity.HIGH, effectiveScriptSrcDirective, value));
            }
            else {
                violations.push(new finding_1.Finding(finding_1.Type.SCRIPT_ALLOWLIST_BYPASS, `No bypass found; make sure that this URL doesn't serve JSONP ` +
                    'replies or Angular libraries.', finding_1.Severity.MEDIUM_MAYBE, effectiveScriptSrcDirective, value));
            }
        }
    });
    return violations;
}
exports.checkScriptAllowlistBypass = checkScriptAllowlistBypass;
function checkFlashObjectAllowlistBypass(parsedCsp) {
    const violations = [];
    const effectiveObjectSrcDirective = parsedCsp.getEffectiveDirective(csp_1.Directive.OBJECT_SRC);
    const objectSrcValues = parsedCsp.directives[effectiveObjectSrcDirective] || [];
    const pluginTypes = parsedCsp.directives[csp_1.Directive.PLUGIN_TYPES];
    if (pluginTypes && !pluginTypes.includes('application/x-shockwave-flash')) {
        return [];
    }
    for (const value of objectSrcValues) {
        if (value === csp_1.Keyword.NONE) {
            return [];
        }
        const url = '//' + utils.getSchemeFreeUrl(value);
        const flashBypass = utils.matchWildcardUrls(url, flash.URLS);
        if (flashBypass) {
            violations.push(new finding_1.Finding(finding_1.Type.OBJECT_ALLOWLIST_BYPASS, flashBypass.hostname +
                ' is known to host Flash files which allow to bypass this CSP.', finding_1.Severity.HIGH, effectiveObjectSrcDirective, value));
        }
        else if (effectiveObjectSrcDirective === csp_1.Directive.OBJECT_SRC) {
            violations.push(new finding_1.Finding(finding_1.Type.OBJECT_ALLOWLIST_BYPASS, `Can you restrict object-src to 'none' only?`, finding_1.Severity.MEDIUM_MAYBE, effectiveObjectSrcDirective, value));
        }
    }
    return violations;
}
exports.checkFlashObjectAllowlistBypass = checkFlashObjectAllowlistBypass;
function looksLikeIpAddress(maybeIp) {
    if (maybeIp.startsWith('[') && maybeIp.endsWith(']')) {
        return true;
    }
    if (/^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$/.test(maybeIp)) {
        return true;
    }
    return false;
}
exports.looksLikeIpAddress = looksLikeIpAddress;
function checkIpSource(parsedCsp) {
    const violations = [];
    const checkIp = (directive, directiveValues) => {
        for (const value of directiveValues) {
            const host = utils.getHostname(value);
            if (looksLikeIpAddress(host)) {
                if (host === '127.0.0.1') {
                    violations.push(new finding_1.Finding(finding_1.Type.IP_SOURCE, directive + ' directive allows localhost as source. ' +
                        'Please make sure to remove this in production environments.', finding_1.Severity.INFO, directive, value));
                }
                else {
                    violations.push(new finding_1.Finding(finding_1.Type.IP_SOURCE, directive + ' directive has an IP-Address as source: ' + host +
                        ' (will be ignored by browsers!). ', finding_1.Severity.INFO, directive, value));
                }
            }
        }
    };
    utils.applyCheckFunktionToDirectives(parsedCsp, checkIp);
    return violations;
}
exports.checkIpSource = checkIpSource;
function checkDeprecatedDirective(parsedCsp) {
    const violations = [];
    if (csp_1.Directive.REFLECTED_XSS in parsedCsp.directives) {
        violations.push(new finding_1.Finding(finding_1.Type.DEPRECATED_DIRECTIVE, 'reflected-xss is deprecated since CSP2. ' +
            'Please, use the X-XSS-Protection header instead.', finding_1.Severity.INFO, csp_1.Directive.REFLECTED_XSS));
    }
    if (csp_1.Directive.REFERRER in parsedCsp.directives) {
        violations.push(new finding_1.Finding(finding_1.Type.DEPRECATED_DIRECTIVE, 'referrer is deprecated since CSP2. ' +
            'Please, use the Referrer-Policy header instead.', finding_1.Severity.INFO, csp_1.Directive.REFERRER));
    }
    if (csp_1.Directive.DISOWN_OPENER in parsedCsp.directives) {
        violations.push(new finding_1.Finding(finding_1.Type.DEPRECATED_DIRECTIVE, 'disown-opener is deprecated since CSP3. ' +
            'Please, use the Cross Origin Opener Policy header instead.', finding_1.Severity.INFO, csp_1.Directive.DISOWN_OPENER));
    }
    return violations;
}
exports.checkDeprecatedDirective = checkDeprecatedDirective;
function checkNonceLength(parsedCsp) {
    const noncePattern = new RegExp('^\'nonce-(.+)\'$');
    const violations = [];
    utils.applyCheckFunktionToDirectives(parsedCsp, (directive, directiveValues) => {
        for (const value of directiveValues) {
            const match = value.match(noncePattern);
            if (!match) {
                continue;
            }
            const nonceValue = match[1];
            if (nonceValue.length < 8) {
                violations.push(new finding_1.Finding(finding_1.Type.NONCE_LENGTH, 'Nonces should be at least 8 characters long.', finding_1.Severity.MEDIUM, directive, value));
            }
            if (!csp.isNonce(value, true)) {
                violations.push(new finding_1.Finding(finding_1.Type.NONCE_CHARSET, 'Nonces should only use the base64 charset.', finding_1.Severity.INFO, directive, value));
            }
        }
    });
    return violations;
}
exports.checkNonceLength = checkNonceLength;
function checkSrcHttp(parsedCsp) {
    const violations = [];
    utils.applyCheckFunktionToDirectives(parsedCsp, (directive, directiveValues) => {
        for (const value of directiveValues) {
            const description = directive === csp_1.Directive.REPORT_URI ?
                'Use HTTPS to send violation reports securely.' :
                'Allow only resources downloaded over HTTPS.';
            if (value.startsWith('http://')) {
                violations.push(new finding_1.Finding(finding_1.Type.SRC_HTTP, description, finding_1.Severity.MEDIUM, directive, value));
            }
        }
    });
    return violations;
}
exports.checkSrcHttp = checkSrcHttp;
function checkHasConfiguredReporting(parsedCsp) {
    const reportUriValues = parsedCsp.directives[csp_1.Directive.REPORT_URI] || [];
    if (reportUriValues.length > 0) {
        return [];
    }
    const reportToValues = parsedCsp.directives[csp_1.Directive.REPORT_TO] || [];
    if (reportToValues.length > 0) {
        return [new finding_1.Finding(finding_1.Type.REPORT_TO_ONLY, `This CSP policy only provides a reporting destination via the 'report-to' directive. This directive is only supported in Chromium-based browsers so it is recommended to also use a 'report-uri' directive.`, finding_1.Severity.INFO, csp_1.Directive.REPORT_TO)];
    }
    return [new finding_1.Finding(finding_1.Type.REPORTING_DESTINATION_MISSING, 'This CSP policy does not configure a reporting destination. This makes it difficult to maintain the CSP policy over time and monitor for any breakages.', finding_1.Severity.INFO, csp_1.Directive.REPORT_URI)];
}
exports.checkHasConfiguredReporting = checkHasConfiguredReporting;

},{"../allowlist_bypasses/angular":2,"../allowlist_bypasses/flash":3,"../allowlist_bypasses/jsonp":4,"../csp":8,"../finding":10,"../utils":12}],7:[function(require,module,exports){
"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    Object.defineProperty(o, k2, { enumerable: true, get: function() { return m[k]; } });
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.checkRequiresTrustedTypesForScripts = exports.checkAllowlistFallback = exports.checkUnsafeInlineFallback = exports.checkStrictDynamicNotStandalone = exports.checkStrictDynamic = void 0;
const csp = __importStar(require("../csp"));
const csp_1 = require("../csp");
const finding_1 = require("../finding");
function checkStrictDynamic(parsedCsp) {
    const directiveName = parsedCsp.getEffectiveDirective(csp.Directive.SCRIPT_SRC);
    const values = parsedCsp.directives[directiveName] || [];
    const schemeOrHostPresent = values.some((v) => !v.startsWith('\''));
    if (schemeOrHostPresent && !values.includes(csp_1.Keyword.STRICT_DYNAMIC)) {
        return [new finding_1.Finding(finding_1.Type.STRICT_DYNAMIC, 'Host allowlists can frequently be bypassed. Consider using ' +
                '\'strict-dynamic\' in combination with CSP nonces or hashes.', finding_1.Severity.STRICT_CSP, directiveName)];
    }
    return [];
}
exports.checkStrictDynamic = checkStrictDynamic;
function checkStrictDynamicNotStandalone(parsedCsp) {
    const directiveName = parsedCsp.getEffectiveDirective(csp.Directive.SCRIPT_SRC);
    const values = parsedCsp.directives[directiveName] || [];
    if (values.includes(csp_1.Keyword.STRICT_DYNAMIC) &&
        (!parsedCsp.policyHasScriptNonces() &&
            !parsedCsp.policyHasScriptHashes())) {
        return [new finding_1.Finding(finding_1.Type.STRICT_DYNAMIC_NOT_STANDALONE, '\'strict-dynamic\' without a CSP nonce/hash will block all scripts.', finding_1.Severity.INFO, directiveName)];
    }
    return [];
}
exports.checkStrictDynamicNotStandalone = checkStrictDynamicNotStandalone;
function checkUnsafeInlineFallback(parsedCsp) {
    if (!parsedCsp.policyHasScriptNonces() &&
        !parsedCsp.policyHasScriptHashes()) {
        return [];
    }
    const directiveName = parsedCsp.getEffectiveDirective(csp.Directive.SCRIPT_SRC);
    const values = parsedCsp.directives[directiveName] || [];
    if (!values.includes(csp_1.Keyword.UNSAFE_INLINE)) {
        return [new finding_1.Finding(finding_1.Type.UNSAFE_INLINE_FALLBACK, 'Consider adding \'unsafe-inline\' (ignored by browsers supporting ' +
                'nonces/hashes) to be backward compatible with older browsers.', finding_1.Severity.STRICT_CSP, directiveName)];
    }
    return [];
}
exports.checkUnsafeInlineFallback = checkUnsafeInlineFallback;
function checkAllowlistFallback(parsedCsp) {
    const directiveName = parsedCsp.getEffectiveDirective(csp.Directive.SCRIPT_SRC);
    const values = parsedCsp.directives[directiveName] || [];
    if (!values.includes(csp_1.Keyword.STRICT_DYNAMIC)) {
        return [];
    }
    if (!values.some((v) => ['http:', 'https:', '*'].includes(v) || v.includes('.'))) {
        return [new finding_1.Finding(finding_1.Type.ALLOWLIST_FALLBACK, 'Consider adding https: and http: url schemes (ignored by browsers ' +
                'supporting \'strict-dynamic\') to be backward compatible with older ' +
                'browsers.', finding_1.Severity.STRICT_CSP, directiveName)];
    }
    return [];
}
exports.checkAllowlistFallback = checkAllowlistFallback;
function checkRequiresTrustedTypesForScripts(parsedCsp) {
    const directiveName = parsedCsp.getEffectiveDirective(csp.Directive.REQUIRE_TRUSTED_TYPES_FOR);
    const values = parsedCsp.directives[directiveName] || [];
    if (!values.includes(csp.TrustedTypesSink.SCRIPT)) {
        return [new finding_1.Finding(finding_1.Type.REQUIRE_TRUSTED_TYPES_FOR_SCRIPTS, 'Consider requiring Trusted Types for scripts to lock down DOM XSS ' +
                'injection sinks. You can do this by adding ' +
                '"require-trusted-types-for \'script\'" to your policy.', finding_1.Severity.INFO, csp.Directive.REQUIRE_TRUSTED_TYPES_FOR)];
    }
    return [];
}
exports.checkRequiresTrustedTypesForScripts = checkRequiresTrustedTypesForScripts;

},{"../csp":8,"../finding":10}],8:[function(require,module,exports){
"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.CspError = exports.isHash = exports.HASH_PATTERN = exports.STRICT_HASH_PATTERN = exports.isNonce = exports.NONCE_PATTERN = exports.STRICT_NONCE_PATTERN = exports.isUrlScheme = exports.isKeyword = exports.isDirective = exports.Version = exports.FETCH_DIRECTIVES = exports.Directive = exports.TrustedTypesSink = exports.Keyword = exports.Csp = void 0;
const finding_1 = require("./finding");
class Csp {
    constructor(directives = {}) {
        this.directives = {};
        for (const [directive, directiveValues] of Object.entries(directives)) {
            if (directiveValues) {
                this.directives[directive] = [...directiveValues];
            }
        }
    }
    clone() {
        return new Csp(this.directives);
    }
    convertToString() {
        let cspString = '';
        for (const [directive, directiveValues] of Object.entries(this.directives)) {
            cspString += directive;
            if (directiveValues !== undefined) {
                for (let value, i = 0; (value = directiveValues[i]); i++) {
                    cspString += ' ';
                    cspString += value;
                }
            }
            cspString += '; ';
        }
        return cspString;
    }
    getEffectiveCsp(cspVersion, optFindings) {
        const findings = optFindings || [];
        const effectiveCsp = this.clone();
        [Directive.SCRIPT_SRC, Directive.SCRIPT_SRC_ATTR, Directive.SCRIPT_SRC_ELEM]
            .forEach(directiveToNormalize => {
            const directive = effectiveCsp.getEffectiveDirective(directiveToNormalize);
            const values = this.directives[directive] || [];
            const effectiveCspValues = effectiveCsp.directives[directive];
            if (effectiveCspValues &&
                (effectiveCsp.policyHasScriptNonces(directive) ||
                    effectiveCsp.policyHasScriptHashes(directive))) {
                if (cspVersion >= Version.CSP2) {
                    if (values.includes(Keyword.UNSAFE_INLINE)) {
                        arrayRemove(effectiveCspValues, Keyword.UNSAFE_INLINE);
                        findings.push(new finding_1.Finding(finding_1.Type.IGNORED, 'unsafe-inline is ignored if a nonce or a hash is present. ' +
                            '(CSP2 and above)', finding_1.Severity.NONE, directive, Keyword.UNSAFE_INLINE));
                    }
                }
                else {
                    for (const value of values) {
                        if (value.startsWith('\'nonce-') || value.startsWith('\'sha')) {
                            arrayRemove(effectiveCspValues, value);
                        }
                    }
                }
            }
            if (effectiveCspValues && this.policyHasStrictDynamic(directive)) {
                if (cspVersion >= Version.CSP3) {
                    for (const value of values) {
                        if (!value.startsWith('\'') || value === Keyword.SELF ||
                            value === Keyword.UNSAFE_INLINE) {
                            arrayRemove(effectiveCspValues, value);
                            findings.push(new finding_1.Finding(finding_1.Type.IGNORED, 'Because of strict-dynamic this entry is ignored in CSP3 and above', finding_1.Severity.NONE, directive, value));
                        }
                    }
                }
                else {
                    arrayRemove(effectiveCspValues, Keyword.STRICT_DYNAMIC);
                }
            }
        });
        if (cspVersion < Version.CSP3) {
            delete effectiveCsp.directives[Directive.REPORT_TO];
            delete effectiveCsp.directives[Directive.WORKER_SRC];
            delete effectiveCsp.directives[Directive.MANIFEST_SRC];
            delete effectiveCsp.directives[Directive.TRUSTED_TYPES];
            delete effectiveCsp.directives[Directive.REQUIRE_TRUSTED_TYPES_FOR];
            delete effectiveCsp.directives[Directive.SCRIPT_SRC_ATTR];
            delete effectiveCsp.directives[Directive.SCRIPT_SRC_ELEM];
            delete effectiveCsp.directives[Directive.STYLE_SRC_ATTR];
            delete effectiveCsp.directives[Directive.STYLE_SRC_ELEM];
        }
        return effectiveCsp;
    }
    getEffectiveDirective(directive) {
        if (directive in this.directives) {
            return directive;
        }
        if ((directive === Directive.SCRIPT_SRC_ATTR ||
            directive === Directive.SCRIPT_SRC_ELEM) &&
            Directive.SCRIPT_SRC in this.directives) {
            return Directive.SCRIPT_SRC;
        }
        if ((directive === Directive.STYLE_SRC_ATTR ||
            directive === Directive.STYLE_SRC_ELEM) &&
            Directive.STYLE_SRC in this.directives) {
            return Directive.STYLE_SRC;
        }
        if (exports.FETCH_DIRECTIVES.includes(directive)) {
            return Directive.DEFAULT_SRC;
        }
        return directive;
    }
    getEffectiveDirectives(directives) {
        const effectiveDirectives = new Set(directives.map((val) => this.getEffectiveDirective(val)));
        return [...effectiveDirectives];
    }
    policyHasScriptNonces(directive) {
        const directiveName = this.getEffectiveDirective(directive || Directive.SCRIPT_SRC);
        const values = this.directives[directiveName] || [];
        return values.some((val) => isNonce(val));
    }
    policyHasScriptHashes(directive) {
        const directiveName = this.getEffectiveDirective(directive || Directive.SCRIPT_SRC);
        const values = this.directives[directiveName] || [];
        return values.some((val) => isHash(val));
    }
    policyHasStrictDynamic(directive) {
        const directiveName = this.getEffectiveDirective(directive || Directive.SCRIPT_SRC);
        const values = this.directives[directiveName] || [];
        return values.includes(Keyword.STRICT_DYNAMIC);
    }
}
exports.Csp = Csp;
var Keyword;
(function (Keyword) {
    Keyword["SELF"] = "'self'";
    Keyword["NONE"] = "'none'";
    Keyword["UNSAFE_INLINE"] = "'unsafe-inline'";
    Keyword["UNSAFE_EVAL"] = "'unsafe-eval'";
    Keyword["WASM_EVAL"] = "'wasm-eval'";
    Keyword["WASM_UNSAFE_EVAL"] = "'wasm-unsafe-eval'";
    Keyword["STRICT_DYNAMIC"] = "'strict-dynamic'";
    Keyword["UNSAFE_HASHED_ATTRIBUTES"] = "'unsafe-hashed-attributes'";
    Keyword["UNSAFE_HASHES"] = "'unsafe-hashes'";
    Keyword["REPORT_SAMPLE"] = "'report-sample'";
    Keyword["BLOCK"] = "'block'";
    Keyword["ALLOW"] = "'allow'";
})(Keyword = exports.Keyword || (exports.Keyword = {}));
var TrustedTypesSink;
(function (TrustedTypesSink) {
    TrustedTypesSink["SCRIPT"] = "'script'";
})(TrustedTypesSink = exports.TrustedTypesSink || (exports.TrustedTypesSink = {}));
var Directive;
(function (Directive) {
    Directive["CHILD_SRC"] = "child-src";
    Directive["CONNECT_SRC"] = "connect-src";
    Directive["DEFAULT_SRC"] = "default-src";
    Directive["FONT_SRC"] = "font-src";
    Directive["FRAME_SRC"] = "frame-src";
    Directive["IMG_SRC"] = "img-src";
    Directive["MEDIA_SRC"] = "media-src";
    Directive["OBJECT_SRC"] = "object-src";
    Directive["SCRIPT_SRC"] = "script-src";
    Directive["SCRIPT_SRC_ATTR"] = "script-src-attr";
    Directive["SCRIPT_SRC_ELEM"] = "script-src-elem";
    Directive["STYLE_SRC"] = "style-src";
    Directive["STYLE_SRC_ATTR"] = "style-src-attr";
    Directive["STYLE_SRC_ELEM"] = "style-src-elem";
    Directive["PREFETCH_SRC"] = "prefetch-src";
    Directive["MANIFEST_SRC"] = "manifest-src";
    Directive["WORKER_SRC"] = "worker-src";
    Directive["BASE_URI"] = "base-uri";
    Directive["PLUGIN_TYPES"] = "plugin-types";
    Directive["SANDBOX"] = "sandbox";
    Directive["DISOWN_OPENER"] = "disown-opener";
    Directive["FORM_ACTION"] = "form-action";
    Directive["FRAME_ANCESTORS"] = "frame-ancestors";
    Directive["NAVIGATE_TO"] = "navigate-to";
    Directive["REPORT_TO"] = "report-to";
    Directive["REPORT_URI"] = "report-uri";
    Directive["BLOCK_ALL_MIXED_CONTENT"] = "block-all-mixed-content";
    Directive["UPGRADE_INSECURE_REQUESTS"] = "upgrade-insecure-requests";
    Directive["REFLECTED_XSS"] = "reflected-xss";
    Directive["REFERRER"] = "referrer";
    Directive["REQUIRE_SRI_FOR"] = "require-sri-for";
    Directive["TRUSTED_TYPES"] = "trusted-types";
    Directive["REQUIRE_TRUSTED_TYPES_FOR"] = "require-trusted-types-for";
    Directive["WEBRTC"] = "webrtc";
})(Directive = exports.Directive || (exports.Directive = {}));
exports.FETCH_DIRECTIVES = [
    Directive.CHILD_SRC, Directive.CONNECT_SRC, Directive.DEFAULT_SRC,
    Directive.FONT_SRC, Directive.FRAME_SRC, Directive.IMG_SRC,
    Directive.MANIFEST_SRC, Directive.MEDIA_SRC, Directive.OBJECT_SRC,
    Directive.SCRIPT_SRC, Directive.SCRIPT_SRC_ATTR, Directive.SCRIPT_SRC_ELEM,
    Directive.STYLE_SRC, Directive.STYLE_SRC_ATTR, Directive.STYLE_SRC_ELEM,
    Directive.WORKER_SRC
];
var Version;
(function (Version) {
    Version[Version["CSP1"] = 1] = "CSP1";
    Version[Version["CSP2"] = 2] = "CSP2";
    Version[Version["CSP3"] = 3] = "CSP3";
})(Version = exports.Version || (exports.Version = {}));
function isDirective(directive) {
    return Object.values(Directive).includes(directive);
}
exports.isDirective = isDirective;
function isKeyword(keyword) {
    return Object.values(Keyword).includes(keyword);
}
exports.isKeyword = isKeyword;
function isUrlScheme(urlScheme) {
    const pattern = new RegExp('^[a-zA-Z][+a-zA-Z0-9.-]*:$');
    return pattern.test(urlScheme);
}
exports.isUrlScheme = isUrlScheme;
exports.STRICT_NONCE_PATTERN = new RegExp('^\'nonce-[a-zA-Z0-9+/_-]+[=]{0,2}\'$');
exports.NONCE_PATTERN = new RegExp('^\'nonce-(.+)\'$');
function isNonce(nonce, strictCheck) {
    const pattern = strictCheck ? exports.STRICT_NONCE_PATTERN : exports.NONCE_PATTERN;
    return pattern.test(nonce);
}
exports.isNonce = isNonce;
exports.STRICT_HASH_PATTERN = new RegExp('^\'(sha256|sha384|sha512)-[a-zA-Z0-9+/]+[=]{0,2}\'$');
exports.HASH_PATTERN = new RegExp('^\'(sha256|sha384|sha512)-(.+)\'$');
function isHash(hash, strictCheck) {
    const pattern = strictCheck ? exports.STRICT_HASH_PATTERN : exports.HASH_PATTERN;
    return pattern.test(hash);
}
exports.isHash = isHash;
class CspError extends Error {
    constructor(message) {
        super(message);
    }
}
exports.CspError = CspError;
function arrayRemove(arr, item) {
    if (arr.includes(item)) {
        const idx = arr.findIndex(elem => item === elem);
        arr.splice(idx, 1);
    }
}

},{"./finding":10}],9:[function(require,module,exports){
"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    Object.defineProperty(o, k2, { enumerable: true, get: function() { return m[k]; } });
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.STRICTCSP_CHECKS = exports.DEFAULT_CHECKS = exports.CspEvaluator = void 0;
const parserChecks = __importStar(require("./checks/parser_checks"));
const securityChecks = __importStar(require("./checks/security_checks"));
const strictcspChecks = __importStar(require("./checks/strictcsp_checks"));
const csp = __importStar(require("./csp"));
class CspEvaluator {
    constructor(parsedCsp, cspVersion, findings) {
        this.findings = [];
        this.version = cspVersion || csp.Version.CSP3;
        this.csp = parsedCsp;
        this.findings = findings || [];
    }
    evaluate(parsedCspChecks, effectiveCspChecks) {
        this.findings = [];
        const checks = effectiveCspChecks || exports.DEFAULT_CHECKS;
        const effectiveCsp = this.csp.getEffectiveCsp(this.version, this.findings);
        if (parsedCspChecks) {
            for (const check of parsedCspChecks) {
                this.findings = this.findings.concat(check(this.csp));
            }
        }
        for (const check of checks) {
            this.findings = this.findings.concat(check(effectiveCsp));
        }
        return this.findings;
    }
}
exports.CspEvaluator = CspEvaluator;
exports.DEFAULT_CHECKS = [
    securityChecks.checkScriptUnsafeInline, securityChecks.checkScriptUnsafeEval,
    securityChecks.checkPlainUrlSchemes, securityChecks.checkWildcards,
    securityChecks.checkMissingDirectives,
    securityChecks.checkScriptAllowlistBypass,
    securityChecks.checkFlashObjectAllowlistBypass, securityChecks.checkIpSource,
    securityChecks.checkNonceLength, securityChecks.checkSrcHttp,
    securityChecks.checkDeprecatedDirective, parserChecks.checkUnknownDirective,
    parserChecks.checkMissingSemicolon, parserChecks.checkInvalidKeyword
];
exports.STRICTCSP_CHECKS = [
    strictcspChecks.checkStrictDynamic,
    strictcspChecks.checkStrictDynamicNotStandalone,
    strictcspChecks.checkUnsafeInlineFallback,
    strictcspChecks.checkAllowlistFallback,
    strictcspChecks.checkRequiresTrustedTypesForScripts
];

},{"./checks/parser_checks":5,"./checks/security_checks":6,"./checks/strictcsp_checks":7,"./csp":8}],10:[function(require,module,exports){
"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Type = exports.Severity = exports.Finding = void 0;
class Finding {
    constructor(type, description, severity, directive, value) {
        this.type = type;
        this.description = description;
        this.severity = severity;
        this.directive = directive;
        this.value = value;
    }
    static getHighestSeverity(findings) {
        if (findings.length === 0) {
            return Severity.NONE;
        }
        const severities = findings.map((finding) => finding.severity);
        const min = (prev, cur) => prev < cur ? prev : cur;
        return severities.reduce(min, Severity.NONE);
    }
    equals(obj) {
        if (!(obj instanceof Finding)) {
            return false;
        }
        return obj.type === this.type && obj.description === this.description &&
            obj.severity === this.severity && obj.directive === this.directive &&
            obj.value === this.value;
    }
}
exports.Finding = Finding;
var Severity;
(function (Severity) {
    Severity[Severity["HIGH"] = 10] = "HIGH";
    Severity[Severity["SYNTAX"] = 20] = "SYNTAX";
    Severity[Severity["MEDIUM"] = 30] = "MEDIUM";
    Severity[Severity["HIGH_MAYBE"] = 40] = "HIGH_MAYBE";
    Severity[Severity["STRICT_CSP"] = 45] = "STRICT_CSP";
    Severity[Severity["MEDIUM_MAYBE"] = 50] = "MEDIUM_MAYBE";
    Severity[Severity["INFO"] = 60] = "INFO";
    Severity[Severity["NONE"] = 100] = "NONE";
})(Severity = exports.Severity || (exports.Severity = {}));
var Type;
(function (Type) {
    Type[Type["MISSING_SEMICOLON"] = 100] = "MISSING_SEMICOLON";
    Type[Type["UNKNOWN_DIRECTIVE"] = 101] = "UNKNOWN_DIRECTIVE";
    Type[Type["INVALID_KEYWORD"] = 102] = "INVALID_KEYWORD";
    Type[Type["NONCE_CHARSET"] = 106] = "NONCE_CHARSET";
    Type[Type["MISSING_DIRECTIVES"] = 300] = "MISSING_DIRECTIVES";
    Type[Type["SCRIPT_UNSAFE_INLINE"] = 301] = "SCRIPT_UNSAFE_INLINE";
    Type[Type["SCRIPT_UNSAFE_EVAL"] = 302] = "SCRIPT_UNSAFE_EVAL";
    Type[Type["PLAIN_URL_SCHEMES"] = 303] = "PLAIN_URL_SCHEMES";
    Type[Type["PLAIN_WILDCARD"] = 304] = "PLAIN_WILDCARD";
    Type[Type["SCRIPT_ALLOWLIST_BYPASS"] = 305] = "SCRIPT_ALLOWLIST_BYPASS";
    Type[Type["OBJECT_ALLOWLIST_BYPASS"] = 306] = "OBJECT_ALLOWLIST_BYPASS";
    Type[Type["NONCE_LENGTH"] = 307] = "NONCE_LENGTH";
    Type[Type["IP_SOURCE"] = 308] = "IP_SOURCE";
    Type[Type["DEPRECATED_DIRECTIVE"] = 309] = "DEPRECATED_DIRECTIVE";
    Type[Type["SRC_HTTP"] = 310] = "SRC_HTTP";
    Type[Type["SRC_NO_PROTOCOL"] = 311] = "SRC_NO_PROTOCOL";
    Type[Type["EXPERIMENTAL"] = 312] = "EXPERIMENTAL";
    Type[Type["WILDCARD_URL"] = 313] = "WILDCARD_URL";
    Type[Type["X_FRAME_OPTIONS_OBSOLETED"] = 314] = "X_FRAME_OPTIONS_OBSOLETED";
    Type[Type["STYLE_UNSAFE_INLINE"] = 315] = "STYLE_UNSAFE_INLINE";
    Type[Type["STATIC_NONCE"] = 316] = "STATIC_NONCE";
    Type[Type["SCRIPT_UNSAFE_HASHES"] = 317] = "SCRIPT_UNSAFE_HASHES";
    Type[Type["STRICT_DYNAMIC"] = 400] = "STRICT_DYNAMIC";
    Type[Type["STRICT_DYNAMIC_NOT_STANDALONE"] = 401] = "STRICT_DYNAMIC_NOT_STANDALONE";
    Type[Type["NONCE_HASH"] = 402] = "NONCE_HASH";
    Type[Type["UNSAFE_INLINE_FALLBACK"] = 403] = "UNSAFE_INLINE_FALLBACK";
    Type[Type["ALLOWLIST_FALLBACK"] = 404] = "ALLOWLIST_FALLBACK";
    Type[Type["IGNORED"] = 405] = "IGNORED";
    Type[Type["REQUIRE_TRUSTED_TYPES_FOR_SCRIPTS"] = 500] = "REQUIRE_TRUSTED_TYPES_FOR_SCRIPTS";
    Type[Type["REPORTING_DESTINATION_MISSING"] = 600] = "REPORTING_DESTINATION_MISSING";
    Type[Type["REPORT_TO_ONLY"] = 601] = "REPORT_TO_ONLY";
})(Type = exports.Type || (exports.Type = {}));

},{}],11:[function(require,module,exports){
"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    Object.defineProperty(o, k2, { enumerable: true, get: function() { return m[k]; } });
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.TEST_ONLY = exports.CspParser = void 0;
const csp = __importStar(require("./csp"));
class CspParser {
    constructor(unparsedCsp) {
        this.csp = new csp.Csp();
        this.parse(unparsedCsp);
    }
    parse(unparsedCsp) {
        this.csp = new csp.Csp();
        const directiveTokens = unparsedCsp.split(';');
        for (let i = 0; i < directiveTokens.length; i++) {
            const directiveToken = directiveTokens[i].trim();
            const directiveParts = directiveToken.match(/\S+/g);
            if (Array.isArray(directiveParts)) {
                const directiveName = directiveParts[0].toLowerCase();
                if (directiveName in this.csp.directives) {
                    continue;
                }
                if (!csp.isDirective(directiveName)) {
                }
                const directiveValues = [];
                for (let directiveValue, j = 1; (directiveValue = directiveParts[j]); j++) {
                    directiveValue = normalizeDirectiveValue(directiveValue);
                    if (!directiveValues.includes(directiveValue)) {
                        directiveValues.push(directiveValue);
                    }
                }
                this.csp.directives[directiveName] = directiveValues;
            }
        }
        return this.csp;
    }
}
exports.CspParser = CspParser;
function normalizeDirectiveValue(directiveValue) {
    directiveValue = directiveValue.trim();
    const directiveValueLower = directiveValue.toLowerCase();
    if (csp.isKeyword(directiveValueLower) || csp.isUrlScheme(directiveValue)) {
        return directiveValueLower;
    }
    return directiveValue;
}
exports.TEST_ONLY = { normalizeDirectiveValue };

},{"./csp":8}],12:[function(require,module,exports){
"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.applyCheckFunktionToDirectives = exports.matchWildcardUrls = exports.getHostname = exports.getSchemeFreeUrl = void 0;
function getSchemeFreeUrl(url) {
    url = url.replace(/^\w[+\w.-]*:\/\//i, '');
    url = url.replace(/^\/\//, '');
    return url;
}
exports.getSchemeFreeUrl = getSchemeFreeUrl;
function getHostname(url) {
    const hostname = new URL('https://' +
        getSchemeFreeUrl(url)
            .replace(':*', '')
            .replace('*', 'wildcard_placeholder'))
        .hostname.replace('wildcard_placeholder', '*');
    const ipv6Regex = /^\[[\d:]+\]/;
    if (getSchemeFreeUrl(url).match(ipv6Regex) && !hostname.match(ipv6Regex)) {
        return '[' + hostname + ']';
    }
    return hostname;
}
exports.getHostname = getHostname;
function setScheme(u) {
    if (u.startsWith('//')) {
        return u.replace('//', 'https://');
    }
    return u;
}
function matchWildcardUrls(cspUrlString, listOfUrlStrings) {
    const cspUrl = new URL(setScheme(cspUrlString
        .replace(':*', '')
        .replace('*', 'wildcard_placeholder')));
    const listOfUrls = listOfUrlStrings.map(u => new URL(setScheme(u)));
    const host = cspUrl.hostname.toLowerCase();
    const hostHasWildcard = host.startsWith('wildcard_placeholder.');
    const wildcardFreeHost = host.replace(/^\wildcard_placeholder/i, '');
    const path = cspUrl.pathname;
    const hasPath = path !== '/';
    for (const url of listOfUrls) {
        const domain = url.hostname;
        if (!domain.endsWith(wildcardFreeHost)) {
            continue;
        }
        if (!hostHasWildcard && host !== domain) {
            continue;
        }
        if (hasPath) {
            if (path.endsWith('/')) {
                if (!url.pathname.startsWith(path)) {
                    continue;
                }
            }
            else {
                if (url.pathname !== path) {
                    continue;
                }
            }
        }
        return url;
    }
    return null;
}
exports.matchWildcardUrls = matchWildcardUrls;
function applyCheckFunktionToDirectives(parsedCsp, check) {
    const directiveNames = Object.keys(parsedCsp.directives);
    for (const directive of directiveNames) {
        const directiveValues = parsedCsp.directives[directive];
        if (directiveValues) {
            check(directive, directiveValues);
        }
    }
}
exports.applyCheckFunktionToDirectives = applyCheckFunktionToDirectives;

},{}]},{},[1]);
