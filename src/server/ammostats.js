/* ammostats.js
 * license: NCSA
 * copyright: Faupi
 * authors:
 * - Faupi
 */

"use strict";

class AmmoStats {
    constructor() {
        this.mod = require("../package.json");
        this.translations = require("../res/translations.json");
        Logger.info(`Loading: ${this.mod.name} ${this.mod.version}`);
        
        ModLoader.onLoad[this.mod.name] = this.init.bind(this);
    }

    init(){
        this.wrapGetImage();
        this.updateLocalization();
    }

    updateLocalization(){
        var globalLocale = DatabaseServer.tables.locales.global;

        for(let language in this.translations){
            if(!language in globalLocale) continue;

            let attrKvPair = this.translations[language];
            for(let attrKey in attrKvPair){
                let attrValue = attrKvPair[attrKey];

                globalLocale[language].interface[attrKey] = attrValue;
            }
        }
    }

    wrapGetImage(){
        this.defaultImageResponse = HttpServer.onRespond["IMAGE"];
        HttpServer.onRespond["IMAGE"] = this.getImage.bind(this);
    }

    getImage(sessionID, req, resp, body)
    {
        const path = `${ModLoader.getModPath(this.mod.name)}res/`;

        if (req.url.includes("/files/armorDamage"))
        {
            HttpServer.sendFile(resp, `${path}armorDamage.png`);
            return;
        }
        else if (req.url.includes("/files/ricochet"))
        {
            HttpServer.sendFile(resp, `${path}ricochet.png`);
            return;
        }

        this.defaultImageResponse(sessionID, req, resp, body);
    }
}

module.exports = new AmmoStats();