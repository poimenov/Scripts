<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:ext="urn:ExtObj">
    <xsl:output method="html" indent="yes" encoding="UTF-8"/>
    
    <xsl:template match="/">
        <html lang="en">
            <head>
                <meta charset="UTF-8" />
                <title><xsl:value-of select="resume/name"/> - <xsl:value-of select="resume/headline"/></title>
                <style>
                    <![CDATA[
                    * {
                        margin: 0;
                        padding: 0;
                        box-sizing: border-box;
                        --primary-color: #1f2937;
                        --bg-color: #ffffff;
                    }    
                    body {
                        font-family: Helvetica Condensed;
                        font-size:14px;
                        line-height: 1.3;
                        min-height: 100vh;
                        padding: 20px;		
                        color: var(--primary-color);
                        background: var(--bg-color);
                    }
                    h1 {
                        font-size:24px;
                    }
                    h2 {
                        font-size:22px;
                    }                    
                    h3 {
                        border-top: 1px solid var(--primary-color);
                        border-bottom: 1px solid var(--primary-color);
                        margin-top: 12px;
                        margin-bottom: 6px;
                        text-align: center;
                        font-size:20px;
                        clear: both;
                    }
                    h4 {
                        font-size:18px;
                    }  
                    h5 {
                        font-size:16px;
                    }                                      
                    p {
                        margin-bottom: 6px;
                    }

                    ul {
                        margin: 6px 0;
                        padding-left: 24px;
                    }

                    i {
                        margin-bottom: 6px;
                    }

                    img {
                        float: left;
                        margin-right: 15px; 
                        margin-bottom: 10px;
                    }                    

                    .mx-auto {
                        max-width: 210mm;
                        margin: 0 auto;
                        width: 100%;
                    }
                    ]]>
                </style>
            </head>
            <body>
                <div class="mx-auto">
                    <xsl:apply-templates select="resume"/>
                </div>
            </body>
        </html>
    </xsl:template>
    
    <xsl:template match="resume">
        <div>
            <div style="text-align:center;">
                <h1><xsl:value-of select="name"/></h1>
                <h4><xsl:value-of select="headline"/></h4>
            </div>
            <xsl:choose>
                <xsl:when test="picturePath != '' and picturePath != 'data:,'">
                    <img src="{picturePath}" alt="Profile picture"/>
                </xsl:when>
                <xsl:otherwise>
                    <svg viewBox="0 0 24 24" fill="currentColor">
                        <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>
                    </svg>
                </xsl:otherwise>
            </xsl:choose>             
            <div>
                <xsl:if test="email != ''">
                    <div>
                        Email: <a href="mailto:{email}" target="_blank"><xsl:value-of select="email"/></a>
                    </div>
                </xsl:if> 
                <xsl:if test="phone != ''">
                    <div>
                        Phone: <a href="tel:{phone}" target="_blank"><xsl:value-of select="phone"/></a>
                    </div>
                </xsl:if>
                <xsl:if test="location != ''">
                    <div>
                        <xsl:value-of select="location"/>
                    </div>
                </xsl:if> 
                <xsl:apply-templates select="links/link"/>              
            </div>

            <xsl:if test="summary">
                <h3>Summary</h3>
                <div>
                    <xsl:value-of select="ext:ConvertToHtml(summary)" disable-output-escaping="yes"/>
                </div>
            </xsl:if>            
            
            <xsl:if test="skills/skill">
                <h3>Skills</h3>
                <div>
                    <xsl:apply-templates select="skills/skill"/>
                </div>
            </xsl:if>

            <xsl:if test="experiences/experience">
                <h3>Experience</h3>
                <div>
                    <xsl:apply-templates select="experiences/experience"/>
                </div>
            </xsl:if>            
            
            <xsl:if test="educations/education">
                <h3>Edication</h3>
                <div>
                    <xsl:apply-templates select="educations/education"/>
                </div>
            </xsl:if>              

            <xsl:if test="certifications/certification">
                <h3>Certifications</h3>
                <div>
                    <xsl:apply-templates select="certifications/certification"/>
                </div>
            </xsl:if> 

            <xsl:if test="languages/language">
                <h3>Languages</h3>
                <div>
                    <xsl:apply-templates select="languages/language"/>
                </div>
            </xsl:if>             
                    
        </div>
    </xsl:template>
    
    <xsl:template match="link">
        <div>
            <a target="_blank" rel="noopener">
                <xsl:attribute name="href">
                    <xsl:value-of select="." />
                </xsl:attribute>
                <xsl:value-of select="." />
            </a>
        </div>    
    </xsl:template>


    <xsl:template match="skill">
        <h5><xsl:value-of select="name"/></h5>
        <xsl:if test="keywords/keyword">
            <div>
                <xsl:apply-templates select="keywords/keyword"/>
            </div>
        </xsl:if>
    </xsl:template>

    <xsl:template match="keyword">
        <i>
            <xsl:value-of select="."/>
            <xsl:if test="position() != last()">, </xsl:if>        
        </i>
    </xsl:template>

    <xsl:template match="certification">
        <h5><xsl:value-of select="title"/></h5>
        <div>
            <xsl:if test="issuer != ''">
                <xsl:value-of select="issuer"/>:
            </xsl:if>  
            <xsl:if test="website != ''">
                <a href="{website/OriginalString}" target="_blank" rel="noopener">               
                    <xsl:value-of select="label"/>
                </a>
            </xsl:if> 
        <xsl:if test="date != ''">
            , <i><xsl:value-of select="date"/></i>
        </xsl:if>                             
        </div>
    </xsl:template>

    <xsl:template match="language">
        <div>
            <xsl:value-of select="name"/>
            <xsl:if test="fluency != ''">
                (<xsl:value-of select="fluency"/>)
            </xsl:if>
        </div>
    </xsl:template>

    <xsl:template match="experience">
        <div >
            <xsl:choose>
                <xsl:when test="website != ''">
                    <a href="{website/OriginalString}" target="_blank" rel="noopener">
                        <h4><xsl:value-of select="company"/></h4>
                    </a>
                </xsl:when>
                <xsl:otherwise>
                    <h4><xsl:value-of select="company"/></h4>
                </xsl:otherwise>
            </xsl:choose>
            <h5><xsl:value-of select="position"/></h5>
            <xsl:if test="location != ''">
                <b><xsl:value-of select="location"/></b>                
            </xsl:if>
            <xsl:if test="period != ''">
                 - <i><xsl:value-of select="period"/></i>
            </xsl:if>   
            <xsl:if test="description != ''">
                <div>
                    <xsl:value-of select="ext:ConvertToHtml(description)" disable-output-escaping="yes"/>
                </div>
            </xsl:if>                     
        </div>
    </xsl:template>

    <xsl:template match="education">
        <div>
            <xsl:choose>
                <xsl:when test="website != ''">
                    <a href="{website}" target="_blank" rel="noopener">
                        <h4><xsl:value-of select="school"/></h4>
                    </a>
                </xsl:when>
                <xsl:otherwise>
                    <h4><xsl:value-of select="school"/></h4>
                </xsl:otherwise>
            </xsl:choose>
            <div>
                <xsl:value-of select="location"/>
                <xsl:if test="location != '' and period != ''"> • </xsl:if>
                <xsl:value-of select="period"/>
            </div> 
            <div>
                <xsl:if test="area != ''">
                    <b><xsl:value-of select="area"/></b> • 
                </xsl:if>
                <xsl:if test="degree != '' or grade != ''">
                    <i>
                        <xsl:value-of select="degree"/>
                        <xsl:if test="grade != ''"> • <xsl:value-of select="grade"/></xsl:if>
                    </i>
                </xsl:if>                
            </div>           
        </div>
    </xsl:template>            
    
</xsl:stylesheet>